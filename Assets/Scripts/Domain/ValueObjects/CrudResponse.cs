using System;

namespace Domain.ValueObjects
{
    /// <summary>
    /// CRUDレスポンス
    /// </summary>
    [Serializable]
    public readonly struct CrudResponse
    {
        public string Action { get; }
        public string Id { get; }
        public string Data { get; }

        // 存在しない状態を表すフラグ
        public bool HasValue { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="action">アクション</param>
        /// <param name="id">ID</param>
        /// <param name="data">データ</param>
        public CrudResponse(string action, string id, string data)
        {
            this.Action = action;
            this.Id = id;
            this.Data = data;
            this.HasValue = true;
        }

        /// <summary>
        /// nullとの比較演算子をオーバーロード
        /// </summary>
        /// <param name="response">レスポンス</param>
        /// <param name="obj">オブジェクト</param>
        /// <returns>true: nullと等しい, false: nullと等しくない</returns>
        public static bool operator ==(CrudResponse response, object obj)
        {
            if (obj is null)
                return !response.HasValue;

            return response.Equals(obj);
        }

        /// <summary>
        /// !=演算子をオーバーロード
        /// </summary>
        /// <param name="response">レスポンス</param>
        /// <param name="obj">オブジェクト</param>
        /// <returns>true: nullと等しくない, false: nullと等しい</returns>
        public static bool operator !=(CrudResponse response, object obj)
        {
            return !(response == obj);
        }

        /// <summary>
        /// Equalsと GetHashCodeのオーバーライド
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        /// <returns>true: 等しい, false: 等しくない</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return !HasValue;

            if (obj is CrudResponse other)
            {
                if (!HasValue && !other.HasValue)
                    return true;

                if (!HasValue || !other.HasValue)
                    return false;

                return string.Equals(Id, other.Id) &&
                       string.Equals(Action, other.Action) &&
                       string.Equals(Data, other.Data);
            }

            return false;
        }

        /// <summary>
        /// GetHashCodeのオーバーライド
        /// </summary>
        /// <returns>ハッシュコード</returns>
        public override int GetHashCode()
        {
            if (!HasValue)
                return 0;

            int hash = 17;
            hash = hash * 23 + (Id?.GetHashCode() ?? 0);
            hash = hash * 23 + (Action?.GetHashCode() ?? 0);
            hash = hash * 23 + (Data?.GetHashCode() ?? 0);
            return hash;
        }

        /// <summary>
        /// デフォルト値（nullに相当する値）を取得するための静的メソッド
        /// </summary>
        /// <returns>デフォルト値</returns>
        public static CrudResponse Default => new();
    }
}
