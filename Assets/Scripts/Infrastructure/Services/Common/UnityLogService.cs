using System;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// Unity のログサービス
    /// </summary>
    /// <remarks>
    /// Unity のログサービスを提供する。
    /// </remarks>
    public class UnityLogService : ILogService
    {
#if UNITY_EDITOR || !LOGGING_DISABLED
        /// <summary>
        /// デバッグログを出力する
        /// </summary>
        /// <param name="message">メッセージ</param>
        public void Debug(string message)
        {
#if NO_DEBUG
            // Call the global custom Debug class (calls will be compiled out)
            global::Debug.Log("[Debug] " + message);
#else
            // Call the standard UnityEngine.Debug class
            UnityEngine.Debug.Log("[Debug] " + message);
#endif
        }

        /// <summary>
        /// 情報ログを出力する
        /// </summary>
        /// <param name="message">メッセージ</param>
        public void Info(string message)
        {
#if NO_DEBUG
            global::Debug.Log("[Info] " + message);
#else
            UnityEngine.Debug.Log("[Info] " + message);
#endif
        }

        /// <summary>
        /// 警告ログを出力する
        /// </summary>
        /// <param name="message">メッセージ</param>
        public void Warning(string message)
        {
#if NO_DEBUG
            global::Debug.LogWarning(message);
#else
            UnityEngine.Debug.LogWarning(message);
#endif
        }

        /// <summary>
        /// エラーログを出力する
        /// </summary>
        /// <param name="message">メッセージ</param>
        public void Error(string message)
        {
#if NO_DEBUG
            global::Debug.LogError(message);
#else
            UnityEngine.Debug.LogError(message);
#endif
        }

        /// <summary>
        /// エラーログを出力する
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="exception">例外</param>
        public void Error(string message, Exception exception)
        {
#if NO_DEBUG
            global::Debug.LogError($"{message}\nException: {exception}");
#else
            UnityEngine.Debug.LogError($"{message}\nException: {exception}");
#endif
        }
#else
        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
        public void Error(string message, Exception exception) { }
#endif
    }
}
