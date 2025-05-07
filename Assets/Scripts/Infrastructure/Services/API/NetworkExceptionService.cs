using System;

namespace Infrastructure.Services
{
    /// <summary>
    /// カスタム例外クラス
    /// </summary>
    [Serializable]
    public sealed class NetworkExceptionService : Exception
    {
        public NetworkExceptionService(string message) : base(message) { }
    }

    /// <summary>
    /// バージョン不一致例外クラス
    /// </summary>
    [Serializable]
    public sealed class VersionMismatchException : Exception
    {
        public VersionMismatchException(string message) : base(message) { }
    }

    /// <summary>
    /// マスターデータ不一致例外クラス
    /// </summary>
    [Serializable]
    public sealed class MasterDataMismatchException : Exception
    {
        public MasterDataMismatchException(string message) : base(message) { }
    }

    /// <summary>
    /// 二重リクエスト例外クラス
    /// </summary>
    [Serializable]
    public sealed class DuplicateRequestException : Exception
    {
        public DuplicateRequestException(string message) : base(message) { }
    }
}

