using System;

namespace Domain.ValueObjects
{
    /// <summary>
    /// アセットロード操作の結果を表すジェネリッククラス。
    /// </summary>
    /// <typeparam name="T">ロードされるアセットの型。</typeparam>
    public class AssetLoadResult<T> where T : UnityEngine.Object
    {
        public bool IsSuccess { get; private set; }
        public T Payload { get; private set; }
        public string ErrorMessage { get; private set; }

        private AssetLoadResult(bool isSuccess, T payload, string errorMessage)
        {
            IsSuccess = isSuccess;
            Payload = payload;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 成功したロード結果を作成します。
        /// </summary>
        public static AssetLoadResult<T> Success(T payload)
        {
            if (payload == null)
            {
                // 成功したがペイロードがnullというのは通常予期しないため、警告を出すか、
                // 例外をスローするか、あるいは失敗として扱うかを検討する必要がある。
                // ここでは、成功として扱うが、エラーメッセージにその旨を記録する例を示す。
                // より厳密には、null ペイロードでの成功を許可しない設計も考えられる。
                return new AssetLoadResult<T>(true, null, "ロード成功ですが、ペイロードがnullです。");
            }
            return new AssetLoadResult<T>(true, payload, null);
        }

        /// <summary>
        /// 失敗したロード結果を作成します。
        /// </summary>
        public static AssetLoadResult<T> Failure(string errorMessage)
        {
            return new AssetLoadResult<T>(false, null, errorMessage ?? "不明なエラーが発生しました。");
        }
    }
}
