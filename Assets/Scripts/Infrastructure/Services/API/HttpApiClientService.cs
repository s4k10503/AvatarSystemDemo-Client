using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

using Domain.Interfaces;
using Domain.ValueObjects;
using Domain.Exceptions;

namespace Infrastructure.Services
{
    /// <summary>
    /// すべてのRESTメソッドをまとめて扱う汎用クラス
    /// </summary>
    /// <remarks>
    /// リトライ、タイムアウト、キャンセル、二重リクエスト防止、バージョンチェックなどを網羅
    /// </remarks>
    public sealed class HttpApiClientService : IRequestSenderService, IDisposable
    {
        private readonly IVersionProviderService _versionProvider;
        private readonly ConcurrentDictionary<string, RequestIdEntry> _processedRequestIds = new();
        private readonly System.Random _random = new();
        private readonly Timer _cleanupTimer;

        // タイムアウト設定をキャッシュ
        private readonly ConcurrentDictionary<float, TimeoutSettings> _timeoutSettingsCache = new();

        // ヘッダー値をキャッシュ
        private readonly string _appVersionHeader;
        private readonly string _masterDataVersionHeader;

        // オブジェクトプール
        private readonly ConcurrentQueue<RequestIdEntry> _requestIdEntryPool = new();
        private const int MAX_POOL_SIZE = 100;

        /// <summary>
        /// リクエストIDエントリ
        /// </summary>
        private class RequestIdEntry
        {
            public string Id { get; set; }
            public DateTime Timestamp { get; set; }

            /// <summary>
            /// リセット
            /// </summary>
            /// <param name="id">リクエストID</param>
            public void Reset(string id)
            {
                Id = id;
                Timestamp = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// タイムアウト設定
        /// </summary>
        public class TimeoutSettings
        {
            public float ConnectTimeout { get; set; } = 5.0f;
            public float ReadTimeout { get; set; } = 30.0f;
            public float WriteTimeout { get; set; } = 30.0f;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="versionProvider">バージョンプロバイダー</param>
        public HttpApiClientService(IVersionProviderService versionProvider)
        {
            _versionProvider = versionProvider;

            // ヘッダー値を事前にキャッシュ
            _appVersionHeader = _versionProvider.AppVersion;
            _masterDataVersionHeader = _versionProvider.MasterDataVersion;

            // 1時間ごとに古いリクエストIDをクリーンアップ
            _cleanupTimer = new Timer(_ => CleanupOldRequestIds(), null,
                TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        /// <summary>
        /// 古いリクエストIDをクリーンアップ
        /// </summary>
        private void CleanupOldRequestIds()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-1);
            var oldEntries = _processedRequestIds
                .Where(kvp => kvp.Value.Timestamp < cutoffTime)
                .ToList();

            foreach (var entry in oldEntries)
            {
                if (_processedRequestIds.TryRemove(entry.Key, out var requestEntry) &&
                    _requestIdEntryPool.Count < MAX_POOL_SIZE)
                {
                    _requestIdEntryPool.Enqueue(requestEntry);
                }
            }
        }

        /// <summary>
        /// リクエストIDエントリを取得
        /// </summary>
        private RequestIdEntry GetRequestIdEntry()
        {
            if (_requestIdEntryPool.TryDequeue(out var entry))
            {
                return entry;
            }
            return new RequestIdEntry();
        }

        /// <summary>
        /// タイムアウト設定を取得
        /// </summary>
        /// <param name="timeoutSeconds">タイムアウト秒数</param>
        /// <returns>タイムアウト設定</returns>
        private TimeoutSettings GetTimeoutSettings(float timeoutSeconds)
        {
            return _timeoutSettingsCache.GetOrAdd(timeoutSeconds, (key) => new TimeoutSettings
            {
                ConnectTimeout = key,
                ReadTimeout = key,
                WriteTimeout = key
            });
        }

        /// <summary>
        /// 次のリトライ遅延を計算
        /// </summary>
        /// <param name="baseInterval">基本遅延</param>
        /// <param name="retryCount">リトライ回数 (0始まり)</param>
        /// <returns>次のリトライ遅延</returns>
        private float CalculateNextRetryDelay(float baseInterval, int retryCount)
        {
            float exponentialDelay = baseInterval * Mathf.Pow(1.5f, retryCount);
            float jitter = (float)(_random.NextDouble() * 0.1 * exponentialDelay);
            return exponentialDelay + jitter;
        }

        /// <summary>
        /// リクエストを送信
        /// </summary>
        public async UniTask<string> SendRequestAsync(
            HttpMethod method,
            string url,
            object data = null,
            int maxRetries = 3,
            float initialInterval = 2.0f,
            float timeoutSeconds = 10.0f,
            CancellationToken ct = default,
            string accessToken = null,
            string externalRequestId = null
        )
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentException("Access token is required for API requests");

            // リクエストIDの決定: 外部指定があればそれを使用、なければ URL+ペイロードハッシュ
            string requestId;
            if (!string.IsNullOrEmpty(externalRequestId))
            {
                requestId = externalRequestId;
            }
            else
            {
                string payloadJson = data != null ? JsonConvert.SerializeObject(data) : string.Empty;
                string combinedData = url + payloadJson; // Combine URL and payload
                using (var sha256 = SHA256.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(combinedData);
                    byte[] hashBytes = sha256.ComputeHash(inputBytes);
                    StringBuilder sb = new();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    requestId = sb.ToString();
                }
            }

            // リクエストIDの重複チェックと追加
            RequestIdEntry requestEntry = null;
            try
            {
                requestEntry = GetRequestIdEntry();
                requestEntry.Reset(requestId); // Use determined requestId
                if (!_processedRequestIds.TryAdd(requestId, requestEntry))
                {
                    Debug.LogWarning($"Duplicate request detected and skipped: [{requestId}] for URL: {url}");
                    throw new DuplicateRequestException($"Request [{requestId}] has already been processed.");
                }

                int attempt = 0;
                while (true) // リトライは attempt と maxRetries で制御
                {
                    UnityWebRequest request = null;
                    try
                    {
                        attempt++;
                        request = SetupRequestInternal(method, url, data, timeoutSeconds, accessToken, requestId);

                        // 通信実行
                        await request.SendWebRequest().ToUniTask(cancellationToken: ct);

                        // --- 成功 ---
                        string responseText = request.downloadHandler.text;
                        request.Dispose();
                        return responseText;
                    }
                    catch (UnityWebRequestException ex)
                    {
                        // UWE を処理 (426チェック、リトライ判断、最終例外スローを含む)
                        bool shouldRetry = await HandleUnityWebRequestExceptionAsync(
                            ex, request, attempt, maxRetries, initialInterval, url, ct
                        );

                        // HandleUnityWebRequestExceptionAsync 内で Dispose されるか、
                        // shouldRetry = false の場合に例外がスローされる
                        if (shouldRetry)
                        {
                            continue; // 次の試行へ
                        }
                        // HandleUnityWebRequestExceptionAsync が false を返し、
                        // かつ例外をスローしなかった場合 (通常はありえないが念のため)
                        throw;
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.Log($"Request cancelled for {url}.");
                        request?.Dispose();
                        throw;
                    }
                    catch (Exception ex) // その他の予期しない例外
                    {
                        Debug.LogError($"Unexpected exception during request to {url}: {ex}");
                        request?.Dispose();
                        throw;
                    }
                } // End while loop
            }
            finally // Ensure requestId is removed after processing
            {
                // リクエスト処理後（成功、失敗、キャンセル問わず）にIDを削除
                if (requestId != null && _processedRequestIds.TryRemove(requestId, out var removedEntry))
                {
                    // Remove successful, return entry to pool if possible
                    if (_requestIdEntryPool.Count < MAX_POOL_SIZE)
                    {
                        _requestIdEntryPool.Enqueue(removedEntry);
                    }
                }
            }
        }

        /// <summary>
        /// UnityWebRequest を生成し、共通ヘッダーとタイムアウトを設定
        /// </summary>
        private UnityWebRequest SetupRequestInternal(
            HttpMethod method, string url, object data, float timeoutSeconds, string accessToken, string requestId)
        {
            var request = CreateUnityWebRequest(method, url, data);
            var timeoutSettings = GetTimeoutSettings(timeoutSeconds);

            request.SetRequestHeader("App-Version", _appVersionHeader);
            request.SetRequestHeader("Master-Data-Version", _masterDataVersionHeader);
            request.SetRequestHeader("Request-Id", requestId);
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SetRequestHeader("X-Requested-With", "Unity");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = Mathf.CeilToInt(timeoutSettings.ConnectTimeout);

            return request;
        }

        /// <summary>
        /// UnityWebRequestException を処理 (426チェック、リトライ判断、最終例外スロー)
        /// </summary>
        /// <returns>リトライすべき場合は true、そうでなければ例外をスロー</returns>
        private async UniTask<bool> HandleUnityWebRequestExceptionAsync(
            UnityWebRequestException ex, UnityWebRequest request,
            int attempt, int maxRetries, float initialInterval, string url,
            CancellationToken ct)
        {
            long statusCode = request?.responseCode ?? 0;
            string errorDetails = $"Status Code: {statusCode}, Error: {ex.Message}, URL: {url}";
            string errorBody = request?.downloadHandler?.text;

            Debug.LogWarning($"UnityWebRequestException caught: {errorDetails}");

            // --- 426 (バージョン不一致) チェック ---
            if (statusCode == 426)
            {
                request?.Dispose();
                // VersionMismatchException コンストラクタに合わせて引数を調整
                throw new VersionMismatchException($"Application version is outdated. Server response: {errorBody}. Details: {errorDetails}");
            }

            // --- リトライ判断 ---
            // attempt は 1 始まりなので、リトライ回数は attempt - 1
            bool canRetry = (attempt - 1) < maxRetries && IsRetryableError(ex, statusCode);

            request?.Dispose(); // リトライ有無に関わらずここで Dispose

            if (canRetry)
            {
                Debug.LogWarning($"Retry {attempt}/{maxRetries} for {url} due to {ex.GetType().Name} ({statusCode}). Details: {errorDetails}");
                float delaySeconds = CalculateNextRetryDelay(initialInterval, attempt - 1); // attemptは1始まりなので-1
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);
                return true; // リトライする
            }
            else // --- リトライ不能 または 最大回数到達 ---
            {
                Debug.LogError($"Request failed permanently or max retries reached ({attempt - 1}/{maxRetries}) for {url}. Details: {errorDetails}");
                if ((attempt - 1) >= maxRetries) // リトライ回数が最大に達した場合
                {
                    // ApiMaxRetriesException コンストラクタに合わせて引数を調整
                    throw new ApiMaxRetriesException($"Max retries ({maxRetries}) reached for {url}. Last error: {errorDetails}");
                }

                // リトライ不能なエラーの場合 (404など)
                HandleHttpError(statusCode, $"Non-retryable error: {errorDetails}");
                // HandleHttpError が適切に throw しない場合のフォールバック
                // ApiException コンストラクタに合わせて引数を調整
                throw new ApiException($"Request failed: {errorDetails}");
            }
        }

        /// <summary>
        /// HTTPエラーを処理
        /// </summary>
        /// <param name="statusCode">ステータスコード</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        private void HandleHttpError(long statusCode, string errorMessage)
        {
            switch (statusCode)
            {
                case 429:
                    throw new ApiException("Rate limit exceeded. Please try again later.");
                case 401:
                    throw new ApiException("Authentication failed. Please check your credentials.");
                case 403:
                    throw new ApiException("Access forbidden. You don't have permission to access this resource.");
                case 404:
                    throw new ApiException("Requested resource not found.");
                case 408:
                    throw new ApiException("Request timed out.");
                case >= 500 and < 600:
                    throw new ApiException($"Server error occurred: {errorMessage}");
                default: // HandleHttpError が呼ばれるのは SendRequestAsync の ProtocolError か非リトライエラーの後なので、これはNetwork系と見なせる
                    throw new NetworkExceptionService($"Request error: {errorMessage}");
            }
        }

        /// <summary>
        /// リトライ可能なエラーかどうかを判断
        /// </summary>
        /// <param name="ex">例外</param>
        /// <param name="statusCode">ステータスコード</param>
        /// <returns>リトライ可能なエラーかどうか</returns>
        private bool IsRetryableError(Exception ex, long statusCode = 0)
        {
            // 接続エラー自体はリトライ対象とする (NetworkExceptionServiceなど)
            if (ex is NetworkExceptionService || (ex is UnityWebRequestException uwe && uwe.Result == UnityWebRequest.Result.ConnectionError))
                return true;

            // 特定のステータスコードに基づくリトライ
            return statusCode switch
            {
                429 => true, // Too Many Requests
                >= 500 and < 600 => true, // Server errors (5xx)
                408 => true, // Request Timeout
                _ => false // Other 4xx errors are typically not retryable
            };
        }

        /// <summary>
        /// 大容量ダウンロード + 進捗監視
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="noProgressTimeout">進捗が変化しない場合のタイムアウト</param>
        /// <param name="ct">キャンセルトークン</param>
        public async UniTask DownloadWithProgressAsync(
            string url,
            float noProgressTimeout = 5.0f,
            CancellationToken ct = default
        )
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                float lastProgress = 0f;
                float startTime = Time.realtimeSinceStartup; // 開始時間を記録
                float lastProgressTime = startTime;

                request.downloadHandler = new DownloadHandlerBuffer();
                var sendOp = request.SendWebRequest();

                while (!sendOp.isDone)
                {
                    // 最初にキャンセルをチェック
                    if (ct.IsCancellationRequested)
                    {
                        request.Abort(); // キャンセルされたらリクエストを中断
                        throw new OperationCanceledException(ct);
                    }

                    // 進捗を確認
                    float currentProgress = request.downloadProgress; // 現在の進捗を取得
                    float currentTime = Time.realtimeSinceStartup; // 現在の時間を取得

                    if (currentProgress > lastProgress)
                    {
                        lastProgress = currentProgress;
                        lastProgressTime = currentTime; // 進捗があれば時間を更新
                    }
                    // 最後の進捗更新からの時間がタイムアウトを超えたかチェック
                    else if (currentTime - lastProgressTime > noProgressTimeout)
                    {
                        request.Abort(); // タイムアウト時にリクエストを中断
                        // テストで期待される TimeoutException をスロー
                        throw new TimeoutException($"Download no progress for {noProgressTimeout}s. Aborting. URL: {url}");
                    }

                    // Unityのメインスレッドが進捗状況を処理・更新できるように Yield
                    await UniTask.Yield(PlayerLoopTiming.Update, ct); // キャンセル可能な Yield
                }

                // ループ終了後: 最終的なリクエスト結果をチェック
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    // リクエストが完了後に失敗した場合 (かつタイムアウトしていない場合) に例外をスロー
                    throw new NetworkExceptionService($"Download failed: {request.error} - Code: {request.responseCode} URL: {url}");
                }
                // ループが終了し、結果が成功ならダウンロード完了
                Debug.Log($"Download completed successfully for URL: {url}");
            }
        }

        /// <summary>
        /// HttpMethodとデータに応じてUnityWebRequestを生成
        /// </summary>
        /// <param name="method">HTTPメソッド</param>
        /// <param name="url">URL</param>
        /// <param name="data">データ</param>
        /// <returns>UnityWebRequest</returns>
        private UnityWebRequest CreateUnityWebRequest(HttpMethod method, string url, object data)
        {
            // 本来は multipart や form-data 等も考慮が必要だが、ここでは簡易化
            switch (method)
            {
                case HttpMethod.GET:
                    return UnityWebRequest.Get(url);

                case HttpMethod.POST:
                    {
                        var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
                        if (data != null)
                        {
                            string json = JsonConvert.SerializeObject(data);
                            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                            req.SetRequestHeader("Content-Type", "application/json");
                        }
                        req.downloadHandler = new DownloadHandlerBuffer();
                        return req;
                    }
                case HttpMethod.PUT:
                    {
                        var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
                        if (data != null)
                        {
                            string json = JsonConvert.SerializeObject(data);
                            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                            req.SetRequestHeader("Content-Type", "application/json");
                        }
                        req.downloadHandler = new DownloadHandlerBuffer();
                        return req;
                    }
                case HttpMethod.PATCH:
                    {
                        // UnityWebRequestにパッチ専用メソッドはないが、カスタムメソッドで対応
                        var req = new UnityWebRequest(url, "PATCH");
                        if (data != null)
                        {
                            string json = JsonConvert.SerializeObject(data);
                            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                            req.SetRequestHeader("Content-Type", "application/json");
                        }
                        req.downloadHandler = new DownloadHandlerBuffer();
                        return req;
                    }
                case HttpMethod.DELETE:
                    {
                        // Deleteでもボディを送る場合はカスタムメソッド化が必要
                        // (一部サーバーやライブラリがDELETE + bodyに対応していない可能性あり)
                        var req = UnityWebRequest.Delete(url);
                        req.downloadHandler = new DownloadHandlerBuffer();
                        return req;
                    }
                default:
                    throw new NotSupportedException($"Method {method} is not supported.");
            }
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _processedRequestIds.Clear();
            _timeoutSettingsCache.Clear();

            // プール内のエントリもクリア
            while (_requestIdEntryPool.TryDequeue(out _)) { }
        }
    }
}
