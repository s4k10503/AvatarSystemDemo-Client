using System;

namespace Domain.ValueObjects
{
    /// <summary>
    /// レコードモデル
    /// </summary>
    [Serializable]
    public readonly struct RecordModel
    {
        [Required(ErrorMessage = "タイトルは必須です。")]
        [StringLength(100, ErrorMessage = "タイトルは1～100文字で入力してください。")]
        public string title { get; }

        [Required(ErrorMessage = "本文は必須です。")]
        public string body { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="body">本文</param>
        public RecordModel(string title, string body)
        {
            this.title = title;
            this.body = body;
        }
    }
}

/// <summary>
/// カスタムバリデーション属性の定義
/// </summary>
public class ValidationAttribute : Attribute
{
    public string ErrorMessage { get; set; }
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
