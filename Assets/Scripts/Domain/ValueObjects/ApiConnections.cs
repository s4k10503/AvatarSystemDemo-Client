namespace Domain.ValueObjects
{
    /// <summary>
    /// API接続情報
    /// </summary>
    public readonly struct ApiConnections
    {
        public string BaseUrl { get; }
        public int MaxRetries { get; }
        public float InitialInterval { get; }
        public float TimeoutSeconds { get; }
        public string AppVersion { get; }
        public string MasterDataVersion { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="baseUrl">ベースURL</param>
        /// <param name="maxRetries">最大リトライ回数</param>
        /// <param name="initialInterval">初期インターバル</param>
        /// <param name="timeoutSeconds">タイムアウト秒数</param>
        public ApiConnections(
            string baseUrl,
            int maxRetries,
            float initialInterval,
            float timeoutSeconds,
            string appVersion,
            string masterDataVersion)
        {
            BaseUrl = baseUrl;
            MaxRetries = maxRetries;
            InitialInterval = initialInterval;
            TimeoutSeconds = timeoutSeconds;
            AppVersion = appVersion;
            MasterDataVersion = masterDataVersion;
        }
    }
}
