using System;

namespace Domain.Interfaces
{
    /// <summary>
    /// ログサービスのインターフェース
    /// </summary>
    public interface ILogService
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Error(string message, Exception exception);
    }
}