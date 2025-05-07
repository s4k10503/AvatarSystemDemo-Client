using System;

namespace Infrastructure.Services
{
    /// <summary>
    /// HTTPリクエストのタイムアウト設定を管理するクラス
    /// </summary>
    public sealed class TimeoutSettings
    {
        public float ConnectTimeout { get; set; } = 5.0f;
        public float ReadTimeout { get; set; } = 30.0f;
        public float WriteTimeout { get; set; } = 30.0f;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TimeoutSettings()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="timeout">タイムアウト</param>
        public TimeoutSettings(float timeout)
        {
            ConnectTimeout = timeout;
            ReadTimeout = timeout;
            WriteTimeout = timeout;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="connectTimeout">接続タイムアウト</param>
        /// <param name="readTimeout">読み込みタイムアウト</param>
        /// <param name="writeTimeout">書き込みタイムアウト</param>
        public TimeoutSettings(float connectTimeout, float readTimeout, float writeTimeout)
        {
            ConnectTimeout = connectTimeout;
            ReadTimeout = readTimeout;
            WriteTimeout = writeTimeout;
        }
    }

    /// <summary>
    /// リクエストIDとそのタイムスタンプを管理するクラス
    /// </summary>
    public class RequestIdEntry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RequestIdEntry()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">リクエストID</param>
        public RequestIdEntry(string id)
        {
            Id = id;
            Timestamp = DateTime.UtcNow;
        }

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
    /// API呼び出しの結果ステータスを表す列挙型
    /// </summary>
    public enum ApiCallStatus
    {
        Success,
        NetworkError,
        ServerError,
        AuthError,
        ClientError,
        Timeout,
        Cancelled
    }
}
