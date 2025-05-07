using System;

namespace Presentation.Interfaces
{
    public interface ILogView
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(string message, Exception exception);
    }
}