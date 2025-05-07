using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using Newtonsoft.Json.Linq;
using System.Net;

using Domain.ValueObjects;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Exceptions;
using Application.DTO;

namespace Application.UseCases
{
    /// <summary>
    /// アバターのカスタマイズ設定に関する機能を提供するユースケースクラス
    /// </summary>
    public sealed class AvatarCustomizationUseCase : IDisposable
    {
        #region プライベートフィールド

        private readonly IAvatarParameterRepository _repository;
        private readonly IAvatarCustomizationService _customizationService;
        private readonly IAvatarSkinColorService _skinColorService;
        private readonly IAvatarHairColorService _hairColorService;
        private readonly IRequestSenderService _apiClientService;
        private readonly ILogService _loggingService;
        private readonly ApiConnections _apiConnections;
        private AvatarCustomizationSettings _currentSettings;
        private AvatarCustomizationSettings _savedSettings;
        private CancellationTokenSource _cts;
        private bool _cloudConnected = false;

        #endregion

        #region コンストラクタ

        /// <summary>
        /// 依存性を注入してAvatarCustomizationUseCaseを初期化します
        /// </summary>
        /// <param name="repository">アバターパラメータリポジトリ</param>
        /// <param name="customizationService">アバターカスタマイズサービス</param>
        /// <param name="skinColorService">肌色サービス</param>
        /// <param name="hairColorService">髪色サービス</param>
        /// <param name="apiClientService">APIクライアントサービス</param>
        /// <param name="loggingService">ログサービス</param>
        /// <param name="apiConnections">API接続情報</param>
        [Inject]
        public AvatarCustomizationUseCase(
            IAvatarParameterRepository repository,
            IAvatarCustomizationService customizationService,
            IAvatarSkinColorService skinColorService,
            IAvatarHairColorService hairColorService,
            IRequestSenderService apiClientService,
            ILogService loggingService,
            ApiConnections apiConnections)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _customizationService = customizationService ?? throw new ArgumentNullException(nameof(customizationService));
            _skinColorService = skinColorService ?? throw new ArgumentNullException(nameof(skinColorService));
            _hairColorService = hairColorService ?? throw new ArgumentNullException(nameof(hairColorService));
            _apiClientService = apiClientService ?? throw new ArgumentNullException(nameof(apiClientService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _apiConnections = apiConnections;

            _currentSettings = null;
            _savedSettings = null;
            _cts = new CancellationTokenSource();
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// アバターカスタマイズ機能を初期化します (リファクタリング版)
        /// </summary>
        /// <param name="isAuthenticated">ユーザーが認証済みかどうか</param>
        /// <param name="accessToken">APIアクセス用のトークン（オプション）</param>
        /// <param name="userId">認証されたユーザーのID（オプション）</param>
        public async UniTask InitializeAsync(bool isAuthenticated = false, string accessToken = null, string userId = null)
        {
            _loggingService.Debug("アバターカスタマイズユースケースを初期化中");
            SetCloudConnectionStatus(false);

            // 1. ローカルから設定読み込み試行 or デフォルト設定
            try
            {
                _currentSettings = await _repository.LoadAsync(_cts.Token);
                _loggingService.Info("ローカルリポジトリから設定を正常に読み込みました。");
                // ローカルデータに userId がない場合、認証情報から設定 (上書きはしない)
                if (isAuthenticated && !string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(_currentSettings.id))
                {
                    _currentSettings.id = userId;
                    _loggingService.Debug($"ユーザーID '{userId}' をローカルから読み込んだ設定（IDなし）に設定しました。");
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _loggingService.Warning($"ローカルリポジトリからの読み込みに失敗しました: {ex.Message}。デフォルト設定を使用します。");
                _currentSettings = new AvatarCustomizationSettings();
                // デフォルト設定の場合、認証情報から userId を設定
                if (isAuthenticated && !string.IsNullOrEmpty(userId))
                {
                    _currentSettings.id = userId;
                    _loggingService.Debug($"ユーザーID '{userId}' をデフォルト設定に設定しました。");
                }
            }

            // 2. 認証済み & オンラインの場合、クラウドから読み込み試行
            if (isAuthenticated && !string.IsNullOrEmpty(accessToken))
            {
                _loggingService.Debug("認証済みでオンラインのため、クラウド操作を試行します。");
                try
                {
                    // 最初に接続をテスト (任意、SendCloudRequestAsyncがリトライを処理)
                    // bool cloudAvailable = await TestCloudConnectionAsync(accessToken);
                    // if (cloudAvailable) { ... } else { SetCloudConnectionStatus(false); }

                    // クラウドからの読み込みを試行
                    _loggingService.Debug("クラウドから設定の読み込みを試行中...");
                    var cloudSettings = await LoadSettingsFromCloudInternalAsync(accessToken, _currentSettings.id); // 利用可能な場合は現在のIDを渡す

                    if (cloudSettings != null)
                    {
                        _loggingService.Info("クラウドから設定を正常に読み込みました。");
                        _currentSettings = cloudSettings; // クラウド設定で上書き
                        SetCloudConnectionStatus(true); // クラウド接続成功

                        // 最新のクラウドバージョンをローカルストレージに保存
                        await SaveToLocalStorageAsync("クラウド同期");
                    }
                    else
                    {
                        // クラウド読み込みがnullを返した（例：404 Not Found、またはdataフィールドが見つからない）
                        _loggingService.Warning("クラウドが有効な設定を返しませんでした。ローカル/デフォルト設定を使用します。");
                        // ローカル/デフォルトから読み込んだ設定を維持
                        SetCloudConnectionStatus(true); // まだ接続中、ただしこのユーザーの特定のデータはまだない
                        // ローカルによると新規ユーザーだった場合、IDがあれば設定されていることを確認
                        if (isAuthenticated && !string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(_currentSettings.id))
                        {
                            _currentSettings.id = userId;
                            await SaveToLocalStorageAsync("クラウドチェック後に初期ユーザーIDを設定");
                        }
                    }
                }
                catch (OperationCanceledException) { throw; } // キャンセルを伝播
                catch (Exception ex)
                {
                    // クラウドエラー（タイムアウト、サーバーエラーなど）を処理
                    _loggingService.Warning($"クラウドからの設定読み込みに失敗しました: {ex.Message}。ローカル/デフォルト設定を使用します。");
                    HandleCloudOperationError(ex, "設定の初期化/読み込み"); // これはCloudConnectionStatusをfalseに設定します
                    // ローカル/デフォルトから読み込んだ設定を維持
                }
            }
            else
            {
                _loggingService.Debug("認証されていないかオフラインです。ローカル/デフォルト設定を使用します。");
                // ステップ1からの設定が使用されます。
            }

            // 3. 最終的な設定を適用 & 保存
            try
            {
                _loggingService.Debug("最終的なカスタマイズ設定を適用中。");
                ApplyCustomization(_currentSettings);
                _savedSettings = _currentSettings.Clone(); // 読み込み/同期後の初期状態を保存
                _loggingService.Info("初期化完了。アバターカスタマイズが適用されました。");
            }
            catch (Exception ex)
            {
                // 最終的な適用中のエラーをログに記録するが、状態が不整合になる可能性あり
                _loggingService.Error("初期化中の最終的なカスタマイズ設定の適用中にエラーが発生しました", ex);
            }
        }

        /// <summary>
        /// アバターに設定されているデフォルトのベースカラーをログに出力します。
        /// </summary>
        public void LogDefaultAvatarColors()
        {
            _loggingService.Debug("デフォルトのアバターカラーをログに出力しようとしています。");

            // --- 肌色 ---
            try
            {
                // サービスが利用可能か確認
                if (_skinColorService == null)
                {
                    _loggingService.Error("SkinColorServiceが利用できません。");
                    return;
                }

                // このメソッドはインターフェースに存在しないため、適切なメソッドに置き換えるか、インターフェースを修正する必要がある
                // var defaultSkinColors = _skinColorService.GetDefaultSkinBaseColors();
                _loggingService.Info("--- デフォルトの肌のベースカラー ---");
                _loggingService.Info("肌色のデフォルト値を取得するには、インターフェースに必要なメソッドを追加してください。");

                /* 以下のコードはインターフェースが更新されたら有効にする
                if (defaultSkinColors.Count > 0)
                {
                    foreach (var kvp in defaultSkinColors)
                    {
                        _loggingService.Info($"マテリアル '{kvp.Key}': {kvp.Value}");
                    }
                }
                else
                {
                    _loggingService.Info("デフォルトの肌色が見つからないか、サービスがレンダラーにアクセスできませんでした。");
                }
                */
            }
            catch (Exception ex)
            {
                _loggingService.Error($"UseCase経由でのデフォルトの肌色の取得エラー: {ex.Message} - {ex.StackTrace}");
            }

            // --- 髪色 ---
            try
            {
                // サービスが利用可能か確認
                if (_hairColorService == null)
                {
                    _loggingService.Error("HairColorServiceが利用できません。");
                    return;
                }

                // このメソッドはインターフェースに存在しないため、適切なメソッドに置き換えるか、インターフェースを修正する必要がある
                // var defaultHairColors = _hairColorService.GetDefaultHairBaseColors();
                _loggingService.Info("--- デフォルトの髪のベースカラー ---");
                _loggingService.Info("髪色のデフォルト値を取得するには、インターフェースに必要なメソッドを追加してください。");

                /* 以下のコードはインターフェースが更新されたら有効にする
                if (defaultHairColors.Count > 0)
                {
                    foreach (var kvp in defaultHairColors)
                    {
                        _loggingService.Info($"マテリアル '{kvp.Key}': {kvp.Value}");
                    }
                }
                else
                {
                    _loggingService.Info("デフォルトの髪色が見つからないか、サービスがレンダラーにアクセスできませんでした。");
                }
                */
            }
            catch (Exception ex)
            {
                _loggingService.Error($"UseCase経由でのデフォルトの髪色の取得エラー: {ex.Message} - {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 現在のアバター設定を取得します
        /// </summary>
        public AvatarSettingsViewModel GetCurrentSettings()
        {
            EnsureInitialized();

            return new AvatarSettingsViewModel
            {
                Height = _currentSettings.Height,
                ShoulderWidth = _currentSettings.ShoulderWidth,
                BodyWidth = _currentSettings.BodyWidth,
                HeadSize = _currentSettings.HeadSize,
                SkinColor = _currentSettings.ToSkinColor(),
                HairColor = _currentSettings.ToHairColor(),
            };
        }

        /// <summary>
        /// 現在の設定に変更があるかを確認します
        /// </summary>
        /// <returns>変更がある場合はtrue</returns>
        public bool HasUnsavedChanges()
        {
            if (_savedSettings == null || _currentSettings == null)
                return false;

            return !_currentSettings.EqualsWithTolerance(_savedSettings);
        }

        /// <summary>
        /// アバターの肌色を更新します（メモリのみ）
        /// </summary>
        public void UpdateSkinColor(SkinColor skinColor)
        {
            _loggingService.Debug($"アバターの肌色をメモリ内で更新中: {skinColor}");
            _currentSettings.UpdateSkinColor(skinColor);
        }

        /// <summary>
        /// アバターの髪色を更新します（メモリのみ）
        /// </summary>
        public void UpdateHairColor(HairColor hairColor)
        {
            _loggingService.Debug($"アバターの髪色をメモリ内で更新中: {hairColor}");
            _currentSettings.UpdateHairColor(hairColor);
        }

        /// <summary>
        /// アバターのカスタマイズ設定を更新します（メモリのみ）
        /// </summary>
        /// <param name="height">身長</param>
        /// <param name="shoulderWidth">肩幅</param>
        /// <param name="bodyWidth">胴体幅</param>
        /// <param name="headSize">頭部サイズ</param>
        /// <param name="skinColor">肌色</param>
        /// <param name="hairColor">髪色</param>
        public void UpdateSettings(
            float height,
            float shoulderWidth,
            float bodyWidth,
            float headSize,
            SkinColor skinColor,
            HairColor hairColor)
        {
            EnsureInitialized();
            _currentSettings.Height = height;
            _currentSettings.ShoulderWidth = shoulderWidth;
            _currentSettings.BodyWidth = bodyWidth;
            _currentSettings.HeadSize = headSize;
            _currentSettings.UpdateSkinColor(skinColor);
            _currentSettings.UpdateHairColor(hairColor);

            // Apply the changes immediately
            ApplyCustomization(_currentSettings);
        }

        /// <summary>
        /// 肌色を現在の値から引き継ぐバージョン
        /// </summary>
        public void UpdateSettings(
            float height,
            float shoulderWidth,
            float bodyWidth,
            float headSize, SkinColor skinColor)
        {
            UpdateSettings(height, shoulderWidth, bodyWidth, headSize, skinColor, _currentSettings.ToHairColor());
        }

        /// <summary>
        /// 髪色を現在の値から引き継ぐバージョン
        /// </summary>
        public void UpdateSettings(float height, float shoulderWidth, float bodyWidth, float headSize)
        {
            UpdateSettings(height, shoulderWidth, bodyWidth, headSize, _currentSettings.ToSkinColor(), _currentSettings.ToHairColor());
        }

        /// <summary>
        /// 現在のカスタマイズ設定をアバターに適用します
        /// </summary>
        public void ApplyCustomization()
        {
            ApplyCustomization(_currentSettings);
        }

        /// <summary>
        /// 指定されたカスタマイズ設定をアバターに適用します
        /// </summary>
        /// <param name="settings">適用する設定</param>
        public void ApplyCustomization(AvatarCustomizationSettings settings)
        {
            if (settings == null)
            {
                _loggingService.Error("カスタマイズ設定がnullです。");
                return;
            }

            try
            {
                // アバターの寸法にBodyScaleの値を適用
                _customizationService.ApplyBodyScale(settings.ToBodyScale());

                // 肌色変更を適用
                _skinColorService.ApplyColor(settings.ToSkinColor());

                // 髪色変更を適用
                _hairColorService.ApplyColor(settings.ToHairColor());

                _loggingService.Info("カスタマイズ設定が正常に適用されました。");
            }
            catch (Exception ex)
            {
                _loggingService.Error("カスタマイズ設定の適用中にエラーが発生しました", ex);
            }
        }

        /// <summary>
        /// 現在設定されている肌色を取得します
        /// </summary>
        public SkinColor GetCurrentSkinColor()
        {
            return _currentSettings.ToSkinColor();
        }

        /// <summary>
        /// 現在設定されている髪色を取得します
        /// </summary>
        public HairColor GetCurrentHairColor()
        {
            return _currentSettings.ToHairColor();
        }

        /// <summary>
        /// 利用可能な肌色プリセットを取得します
        /// </summary>
        public SkinColor[] GetSkinColorPresets()
        {
            return SkinColor.GetPresets();
        }

        /// <summary>
        /// 利用可能な髪色プリセットを取得します
        /// </summary>
        public HairColor[] GetHairColorPresets()
        {
            return HairColor.GetPresets();
        }

        /// <summary>
        /// 設定をローカルストレージに保存します
        /// </summary>
        /// <param name="operationName">オペレーション名（ログ用）</param>
        private async UniTask SaveToLocalStorageAsync(string operationName = "")
        {
            string operation = string.IsNullOrEmpty(operationName) ? "" : $" {operationName} 中";
            try
            {
                _loggingService.Debug($"設定をローカルストレージに保存中{operation}");

                await _repository.SaveAsync(_currentSettings, _cts.Token);
                _loggingService.Debug($"設定がローカルストレージに正常に保存されました{operation}");
            }
            catch (OperationCanceledException)
            {
                _loggingService.Debug($"ローカルストレージへの保存操作がキャンセルされました{operation}");
                throw;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"ローカルストレージへの設定保存に失敗しました{operation}: {ex.Message}");
                throw; // 保存が失敗した場合は例外を再スロー
            }
        }

        /// <summary>
        /// 設定をローカルとクラウド両方に保存します
        /// </summary>
        /// <param name="accessToken">APIアクセス用のトークン（オプション）</param>
        public async UniTask SaveSettingsAsync(string accessToken = null)
        {
            try
            {
                _loggingService.Debug("SaveSettingsAsyncを開始");

                // まずローカルに保存
                await SaveToLocalStorageAsync("SaveSettings");

                // クラウド接続がある場合のみクラウドに保存
                if (_cloudConnected)
                {
                    await SaveToCloudAsync(accessToken);
                }
                else
                {
                    _loggingService.Info("オフラインモードで動作中、設定はローカルのみに保存されました");
                }

                // 保存成功後、現在の設定を保存された設定として更新
                _savedSettings = _currentSettings.Clone();
            }
            catch (OperationCanceledException)
            {
                _loggingService.Info("設定の保存操作がキャンセルされました");
                throw;
            }
        }

        /// <summary>
        /// アバターの設定をデフォルト値にリセットします
        /// </summary>
        /// <param name="accessToken">APIアクセス用のトークン（オプション）</param>
        public async UniTask ResetToDefaultAsync(string accessToken = null)
        {
            try
            {
                _loggingService.Debug("アバターのカスタマイズをデフォルト値にリセット中");
                _currentSettings = new AvatarCustomizationSettings();

                // リポジトリに初期保存
                await SaveToLocalStorageAsync("リセット初期化");

                // クラウド接続時は古いデータを削除
                if (_cloudConnected && !string.IsNullOrEmpty(_currentSettings.id))
                {
                    await DeleteSettingsFromCloudAsync(accessToken);
                }

                // IDをクリアして完了保存
                _currentSettings.id = null;
                await SaveToLocalStorageAsync("リセット完了");

                // デフォルト設定を適用 (EyeColorもリセットされる)
                // 最終的に設定を適用
                ApplyCustomization(_currentSettings);
            }
            catch (OperationCanceledException)
            {
                _loggingService.Info("リセット操作がキャンセルされました");
                throw;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"設定のリセットに失敗しました: {ex.Message}");
                SetCloudConnectionStatus(false);

                // エラー後は現在設定を適用
                ApplyCustomization(_currentSettings);
            }
        }

        /// <summary>
        /// クラウドから設定を削除します
        /// </summary>
        /// <param name="accessToken">APIアクセス用のトークン（オプション）</param>
        private async UniTask DeleteSettingsFromCloudAsync(string accessToken = null)
        {
            try
            {
                string deleteUrl = $"{_apiConnections.BaseUrl}?id={_currentSettings.id}";
                _loggingService.Debug($"ID: {_currentSettings.id} の設定をクラウドから削除中, URL: {deleteUrl}");

                await SendCloudRequestAsync(HttpMethod.DELETE, deleteUrl, null, accessToken);
                _loggingService.Info("クラウドから設定を正常に削除しました");
            }
            catch (OperationCanceledException)
            {
                _loggingService.Info("削除操作がキャンセルされました");
                throw;
            }
            catch (Exception ex)
            {
                HandleCloudOperationError(ex, "設定の削除");
            }
        }

        /// <summary>
        /// 初期化エラーを処理します
        /// </summary>
        private async UniTask HandleInitializationError(Exception ex)
        {
            _loggingService.Error("アバターカスタマイズの初期化に失敗しました", ex);

            // クラウド接続をオフに
            SetCloudConnectionStatus(false);

            // ローカルからのロードを試みる
            await LoadSettingsFromLocalFallback();
        }

        /// <summary>
        /// ローカルストレージから設定をフォールバックとして読み込みます
        /// </summary>
        private async UniTask LoadSettingsFromLocalFallback()
        {
            try
            {
                _loggingService.Info("フォールバックとしてローカルストレージから設定の読み込みを試行中");
                _currentSettings = await _repository.LoadAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _loggingService.Info("読み込み操作がキャンセルされました");
                throw;
            }
            catch (Exception loadEx)
            {
                _loggingService.Error("ローカルストレージからの読み込みに失敗しました、デフォルト設定を使用します", loadEx);
                _currentSettings = new AvatarCustomizationSettings();
            }
        }

        /// <summary>
        /// クラウド接続状態を取得します
        /// </summary>
        public bool IsCloudConnected()
        {
            return _cloudConnected;
        }

        /// <summary>
        /// クラウド接続状態を設定します
        /// </summary>
        /// <param name="status">接続状態（true: 接続中, false: 未接続）</param>
        public void SetCloudConnectionStatus(bool status)
        {
            if (_cloudConnected != status)
            {
                _cloudConnected = status;
                _loggingService.Info($"クラウド接続ステータスが次のように変更されました: {status}");
            }
        }

        /// <summary>
        /// リソースを解放します
        /// </summary>
        public void Dispose()
        {
            _loggingService.Debug("アバターカスタマイズユースケースを破棄中");

            if (_cts != null)
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
                _cts.Dispose();
                _cts = null;
            }
        }

        #endregion

        #region プライベートメソッド

        // GetHeadPositionで使用する一般的なヘッドボーン名のリスト
        private static readonly string[] _headBoneNames = {
            "Head", "head", "mixamorig:Head", "Bip001 Head",
            "Armature/Hips/Spine/Chest/Neck/Head", "Neck/Head"
        };

        /// <summary>
        /// 初期化が完了していることを確認します
        /// </summary>
        private void EnsureInitialized()
        {
            if (_currentSettings == null)
            {
                _loggingService.Warning("アバターカスタマイズユースケースが初期化されていません。デフォルト設定を使用します。");
                _currentSettings = new AvatarCustomizationSettings();
            }
        }

        /// <summary>
        /// アバターモデルから頭部の位置情報を取得する
        /// </summary>
        /// <param name="avatarRoot">アバターのルートオブジェクト</param>
        /// <returns>頭部の位置情報（相対位置とグローバル位置のタプル）、取得できない場合はnull</returns>
        public (Vector3 localOffset, Vector3 worldPosition)? GetHeadPosition(GameObject avatarRoot)
        {
            if (avatarRoot == null) return null;

            try
            {
                // 1. アニメーターからヒューマノイドのヘッドボーンを取得
                Transform headBone = null;
                Animator animator = avatarRoot.GetComponent<Animator>();

                if (animator != null && animator.isHuman)
                {
                    headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                    if (headBone != null)
                    {
                        _loggingService.Debug($"Animator経由でヘッドボーンが見つかりました: {headBone.name}");
                    }
                }

                // 2. ヘッドボーンが見つからない場合は一般的な名前で検索
                if (headBone == null)
                {
                    // static readonly配列を使用
                    foreach (string boneName in _headBoneNames)
                    {
                        // 直接の子を検索
                        headBone = avatarRoot.transform.Find(boneName);
                        if (headBone != null) break;

                        // 再帰的に検索
                        headBone = FindChildRecursively(avatarRoot.transform, boneName);
                        if (headBone != null) break;
                    }

                    if (headBone != null)
                    {
                        _loggingService.Debug($"名前検索でヘッドボーンが見つかりました: {headBone.name}");
                    }
                }

                // 3. ヘッドボーンが見つかった場合
                if (headBone != null)
                {
                    Vector3 localOffset = headBone.position - avatarRoot.transform.position;

                    // 頭の大きさによる調整
                    // Vector3の演算は構造体のため通常ボックス化されない
                    localOffset.y *= _currentSettings.HeadSize;

                    // 実際のカメラ注視点として少し上を見るよう調整
                    Vector3 adjustedWorldPosition = headBone.position + Vector3.up * 0.1f;

                    // ログ出力の文字列補間はボックス化/アロケーションの可能性あり (ILogServiceの実装次第)
                    _loggingService.Debug($"頭の位置 - ローカルオフセット: {localOffset}, ワールド: {adjustedWorldPosition}");
                    return (localOffset, adjustedWorldPosition);
                }

                // 4. ヘッドボーンが見つからない場合は体の高さから推定
                // GetComponentInChildrenはアロケーションの可能性あるが、頻度が低ければ許容範囲
                Renderer renderer = avatarRoot.GetComponentInChildren<SkinnedMeshRenderer>()
                                    ?? avatarRoot.GetComponentInChildren<MeshRenderer>() as Renderer;

                if (renderer != null)
                {
                    // bounds.sizeやVector3演算は通常アロケーション少ない
                    float avatarHeight = renderer.bounds.size.y;
                    float headHeight = avatarHeight * 0.85f * _currentSettings.HeadSize;
                    Vector3 localOffset = new Vector3(0, headHeight, 0);
                    Vector3 worldPosition = avatarRoot.transform.position + localOffset;

                    _loggingService.Debug($"レンダラーから推定された頭の位置 - 高さ: {avatarHeight}, 頭のオフセット: {localOffset}");
                    return (localOffset, worldPosition);
                }

                // 5. 最終フォールバック：カスタマイズ設定から推定
                // float演算、Vector3生成/演算は通常アロケーション少ない
                float estimatedHeight = _currentSettings.Height * 1.8f; // 身長設定から実際の高さを推定
                float estimatedHeadHeight = estimatedHeight * 0.85f * _currentSettings.HeadSize;
                Vector3 estimatedOffset = new(0, estimatedHeadHeight, 0);
                Vector3 estimatedWorldPosition = avatarRoot.transform.position + estimatedOffset;

                _loggingService.Debug($"設定から推定された頭の位置 - 高さ: {estimatedHeight}, 頭のオフセット: {estimatedOffset}");
                return (estimatedOffset, estimatedWorldPosition);
            }
            catch (Exception ex)
            {
                // 例外処理時のログ出力
                _loggingService.Error($"頭の位置の取得エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 指定した名前を含む子オブジェクトを再帰的に検索する
        /// </summary>
        private Transform FindChildRecursively(Transform parent, string nameContains)
        {
            if (parent == null) return null;

            // foreachループは通常、IEnumeratorのアロケーションを伴うが、
            // TransformのGetEnumeratorは最適化されていることが多い。
            foreach (Transform child in parent)
            {
                // StringComparison.OrdinalIgnoreCase はカルチャ依存せず、比較的高速
                if (child.name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                // 再帰呼び出し
                Transform found = FindChildRecursively(child, nameContains);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// クラウドから設定を読み込む内部ヘルパーメソッド
        /// </summary>
        /// <param name="accessToken">APIアクセス用のトークン</param>
        /// <param name="currentUserId">現在メモリにあるユーザーID（null許容）</param>
        /// <returns>クラウドから読み込んだ設定。見つからない、またはエラーの場合はnull</returns>
        private async UniTask<AvatarCustomizationSettings> LoadSettingsFromCloudInternalAsync(string accessToken, string currentUserId)
        {
            // catchブロックでアクセスできるようにurlをtryブロックの外で宣言
            string url = !string.IsNullOrEmpty(currentUserId)
                ? $"{_apiConnections.BaseUrl}?id={currentUserId}"
                : _apiConnections.BaseUrl;

            try
            {
                _loggingService.Debug($"URLからクラウド設定をリクエスト中: {url}");
                string response = await SendCloudRequestAsync(HttpMethod.GET, url, null, accessToken);

                // JSONレスポンス全体を解析
                var jsonObject = JObject.Parse(response);

                // ネストされた "data" オブジェクトを抽出
                JToken dataToken = jsonObject["data"];

                if (dataToken != null && dataToken.Type != JTokenType.Null) // nullトークンもチェック
                {
                    // ネストされた "data" オブジェクトのみをデシリアライズ
                    var cloudSettings = dataToken.ToObject<AvatarCustomizationSettings>();

                    if (cloudSettings != null)
                    {
                        // クラウドデータにIDがない場合、現在のユーザーIDを保持
                        if (string.IsNullOrEmpty(cloudSettings.id) && !string.IsNullOrEmpty(currentUserId))
                        {
                            cloudSettings.id = currentUserId;
                        }
                        return cloudSettings;
                    }
                    else
                    {
                        _loggingService.Warning("クラウドレスポンスからネストされた 'data' オブジェクトのデシリアライズに失敗しました。");
                        return null;
                    }
                }
                else
                {
                    _loggingService.Warning($"'{url}' からのクラウドレスポンスJSONに 'data' フィールドが見つからないかnullです。設定はまだ存在しないと仮定します。");
                    return null; // dataフィールドがないか明示的にnullの場合はnullを返す
                }
            }
            catch (ApiException apiEx) // まず一般的なApiExceptionをキャッチ
            {
                // 例外メッセージまたは内部例外が404を示しているか確認
                bool isNotFound = apiEx.Message.Contains("404") || apiEx.Message.Contains("Not Found");

                if (isNotFound)
                {
                    _loggingService.Info($"URL: {url} のクラウド設定が見つかりませんでした (404)。ユーザーにはまだ保存されたクラウドデータがない可能性があります。");
                    return null; // データが見つからないことを示すためにnullを返す
                }
                else
                {
                    // 他のApiExceptionは呼び出し元で処理するために再スロー
                    throw;
                }
            }
        }

        /// <summary>
        /// 設定をクラウドに保存します
        /// </summary>
        /// <param name="accessToken">APIアクセス用のトークン（オプション）</param>
        private async UniTask SaveToCloudAsync(string accessToken = null)
        {
            try
            {
                var cloudId = await UploadSettingsToCloud(accessToken);

                // IDが更新された場合は保存
                if (cloudId != null && _currentSettings.id != cloudId)
                {
                    _currentSettings.id = cloudId;
                    await _repository.SaveAsync(_currentSettings, _cts.Token); // リポジトリを直接使用
                    _loggingService.Debug($"新しいクラウドIDでローカル設定を更新しました: {cloudId}");
                }
            }
            catch (OperationCanceledException)
            {
                _loggingService.Info("保存操作がキャンセルされました");
                throw;
            }
            catch (Exception ex)
            {
                HandleCloudOperationError(ex, "設定の保存");
            }
        }

        /// <summary>
        /// 設定をクラウドにアップロードします
        /// </summary>
        /// <param name="accessToken">APIアクセス用のトークン（オプション）</param>
        /// <returns>クラウドから返却されたID、失敗時はnull</returns>
        private async UniTask<string> UploadSettingsToCloud(string accessToken = null)
        {
            bool isNewSettings = _currentSettings.id == null;
            HttpMethod method = isNewSettings ? HttpMethod.POST : HttpMethod.PUT;
            string url = isNewSettings ? _apiConnections.BaseUrl : $"{_apiConnections.BaseUrl}?id={_currentSettings.id}";

            // _currentSettingsを直接渡す（HttpApiClientServiceはJsonConvertを使用）
            var response = await SendCloudRequestAsync(method, url, _currentSettings, accessToken);

            // 注意：POST/PUTのサーバーレスポンスはネストされた形式である可能性があります。
            // レスポンスが単に { "id": "..." } でない場合は解析を調整してください。
            // POST/PUTのレスポンス形式がGETと同様に {"data": {"id": "..."}} であると仮定
            try
            {
                var jsonObject = JObject.Parse(response);
                JToken dataToken = jsonObject["data"];
                if (dataToken != null && dataToken["id"] != null)
                {
                    return dataToken["id"].ToString();
                }
                _loggingService.Warning($"クラウド保存レスポンスの 'data' フィールドから 'id' を解析できませんでした。レスポンス: {response}");
            }
            catch (Exception parseEx)
            {
                _loggingService.Warning($"クラウド保存レスポンスの解析に失敗しました: {parseEx.Message}。レスポンス: {response}");
            }


            return null;
        }

        /// <summary>
        /// クラウドにAPIリクエストを送信します
        /// </summary>
        /// <param name="method">HTTPメソッド</param>
        /// <param name="url">リクエストURL</param>
        /// <param name="data">送信データ（オプション）</param>
        /// <param name="accessToken">APIアクセス用のトークン（オプション）</param>
        /// <returns>APIレスポンス</returns>
        private async UniTask<string> SendCloudRequestAsync(HttpMethod method, string url, object data = null, string accessToken = null)
        {
            return await _apiClientService.SendRequestAsync(
                method,
                url,
                data,
                _apiConnections.MaxRetries,
                _apiConnections.InitialInterval,
                _apiConnections.TimeoutSeconds,
                _cts.Token,
                accessToken
            );
        }

        /// <summary>
        /// クラウド操作のエラーを処理します
        /// </summary>
        /// <param name="ex">発生した例外</param>
        /// <param name="operationName">操作名（ログ用）</param>
        private void HandleCloudOperationError(Exception ex, string operationName)
        {
            if (ex is OperationCanceledException)
                throw ex; // キャンセルを伝播

            // 特定のAPI例外が存在する場合はログに記録し、それ以外の場合は一般的な例外をログに記録
            if (ex is ApiOperationCanceledException apiCanceledEx)
                _loggingService.Warning($"{operationName} 中にAPI操作がキャンセルされました: {apiCanceledEx.Message}");
            else if (ex is ApiMaxRetriesException apiRetriesEx)
                _loggingService.Warning($"{operationName} 中にAPIの最大リトライ回数に達しました: {apiRetriesEx.Message}");
            else if (ex is ApiException apiEx) // 基本的なApiExceptionをキャッチ
                _loggingService.Warning($"{operationName} 中のAPIエラー: {apiEx.Message}");
            else // その他の予期しない例外をキャッチ
                _loggingService.Error($"クラウド操作 '{operationName}' 中の予期しないエラー: {ex.Message}", ex);

            SetCloudConnectionStatus(false); // エラー時にクラウドステータスをオフラインに設定
        }

        /// <summary>
        /// 操作をキャンセルします
        /// </summary>
        public void CancelOperations()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _loggingService.Debug("保留中のすべての操作をキャンセル中");
                _cts.Cancel();

                // 新しいトークンを作成して後続の操作が実行できるようにする
                _cts = new CancellationTokenSource();
            }
        }

        /// <summary>
        /// クラウド接続が可能かテストする
        /// </summary>
        /// <param name="accessToken">APIアクセス用のトークン</param>
        /// <returns>接続可能ならtrue、それ以外はfalse</returns>
        private async UniTask<bool> TestCloudConnectionAsync(string accessToken)
        {
            try
            {
                // シンプルなGETリクエストでサーバー接続を試行
                string testUrl = "";
                await SendCloudRequestAsync(HttpMethod.GET, testUrl, null, accessToken);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Warning($"クラウド接続テストに失敗しました: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}

