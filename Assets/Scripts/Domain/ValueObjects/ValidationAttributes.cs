using System;

namespace Domain.ValueObjects
{
    /// <summary>
    /// バリデーション属性
    /// </summary>
    public class ValidationAttribute : Attribute
    {
        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 有効性を確認します。
        /// </summary>
        public virtual bool IsValid(object value) => true;
    }
    /// <summary>
    /// 必須属性
    /// </summary>
    public class RequiredAttribute : ValidationAttribute
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RequiredAttribute()
        {
            ErrorMessage = "この項目は必須です。";
        }

        /// <summary>
        /// 有効性を確認します。
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>有効かどうか</returns>
        public override bool IsValid(object value)
        {
            return value != null && !string.IsNullOrWhiteSpace(value.ToString());
        }
    }

    /// <summary>
    /// 文字列長属性
    /// </summary>
    public class StringLengthAttribute : ValidationAttribute
    {
        private readonly int _maxLength;
        private readonly int _minLength;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="maxLength">最大長</param>
        /// <param name="minLength">最小長</param>
        public StringLengthAttribute(int maxLength, int minLength = 0)
        {
            _maxLength = maxLength;
            _minLength = minLength;
        }

        /// <summary>
        /// 有効性を確認します。
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>有効かどうか</returns>
        public override bool IsValid(object value)
        {
            if (value == null) return _minLength == 0;
            string str = value.ToString();
            return str.Length >= _minLength && str.Length <= _maxLength;
        }
    }
}
