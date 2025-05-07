using System;

namespace Domain.Exceptions
{
    /// <summary>
    /// API例外
    /// </summary>
    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
    }

    /// <summary>
    /// API操作キャンセル例外
    /// </summary>
    public class ApiOperationCanceledException : ApiException
    {
        public ApiOperationCanceledException(string message) : base(message) { }
    }

    /// <summary>
    /// API最大リトライ回数例外
    /// </summary>
    public class ApiMaxRetriesException : ApiException
    {
        public ApiMaxRetriesException(string message) : base(message) { }
    }
}
