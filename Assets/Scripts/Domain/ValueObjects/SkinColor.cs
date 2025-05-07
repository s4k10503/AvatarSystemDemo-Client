using System;

namespace Domain.ValueObjects
{
    /// <summary>
    /// アバターの肌の色
    /// </summary>
    [Serializable]
    public readonly struct SkinColor : IEquatable<SkinColor>
    {
        public readonly ColorValue Value; // Use ColorValue

        /// <summary>
        /// ColorValueを使用してSkinColorを作成
        /// </summary>
        /// <param name="value">色の値</param>
        public SkinColor(ColorValue value)
        {
            Value = value;
        }

        /// <summary>
        /// RGB値を指定してSkinColorを作成
        /// </summary>
        /// <param name="r">赤</param>
        /// <param name="g">緑</param>
        /// <param name="b">青</param>
        /// <param name="a">透明度</param>
        public SkinColor(float r, float g, float b, float a = 1.0f)
            : this(new ColorValue(r, g, b, a))
        {
        }

        /// <summary>
        /// デフォルトの肌色を返す
        /// </summary>
        public static readonly SkinColor Default = new(new ColorValue(0.96f, 0.80f, 0.69f, 1.0f));

        /// <summary>
        /// 利用可能な肌色のプリセットを取得
        /// </summary>
        private static readonly SkinColor[] _presets = {
            new(new ColorValue(0.96f, 0.80f, 0.69f)), // 標準的な肌色
            new(new ColorValue(1.00f, 0.87f, 0.73f)), // 明るい肌色
            new(new ColorValue(0.88f, 0.67f, 0.60f)), // 中間的な肌色
            new(new ColorValue(0.55f, 0.37f, 0.24f)), // 濃い肌色
            new(new ColorValue(0.40f, 0.25f, 0.15f)), // 非常に濃い肌色
        };

        /// <summary>
        /// プリセットを取得
        /// </summary>
        public static SkinColor[] GetPresets()
        {
            return _presets;
        }

        /// <summary>
        /// 文字列形式の表示
        /// </summary>
        public override string ToString()
        {
            return $"SkinColor({Value})";
        }

        /// <summary>
        /// 等価性を確認
        /// </summary>
        public bool Equals(SkinColor other)
        {
            return Value.Equals(other.Value);
        }

        /// <summary>
        /// 等価性を確認
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is SkinColor other && Equals(other);
        }

        /// <summary>
        /// ハッシュコードを取得
        /// </summary>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// 等価性を確認
        /// </summary>
        public static bool operator ==(SkinColor left, SkinColor right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 等価性を確認
        /// </summary>
        public static bool operator !=(SkinColor left, SkinColor right)
        {
            return !(left == right);
        }
    }
}
