using System;

namespace Domain.ValueObjects
{
    /// <summary>
    /// 色を表す値オブジェクト
    /// </summary>
    [Serializable] // シリアライズのために追加
    public readonly struct ColorValue : IEquatable<ColorValue>
    {
        public readonly float R;
        public readonly float G;
        public readonly float B;
        public readonly float A;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="r">赤</param>
        /// <param name="g">緑</param>
        /// <param name="b">青</param>
        /// <param name="a">透明度</param>
        public ColorValue(float r, float g, float b, float a = 1.0f)
        {
            R = Clamp01(r);
            G = Clamp01(g);
            B = Clamp01(b);
            A = Clamp01(a);
        }

        /// <summary>
        /// 0から1の範囲にクランプ
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>クランプされた値</returns>
        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        /// <summary>
        /// 等価性を確認
        /// </summary>
        /// <param name="other">比較対象のColorValue</param>
        /// <returns>等しい場合はtrue、それ以外はfalse</returns>
        public bool Equals(ColorValue other)
        {
            return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
        }

        /// <summary>
        /// オブジェクトの等価性を確認
        /// </summary>
        /// <param name="obj">比較対象のオブジェクト</param>
        /// <returns>等しい場合はtrue、それ以外はfalse</returns>
        public override bool Equals(object obj)
        {
            return obj is ColorValue other && Equals(other);
        }

        /// <summary>
        /// ハッシュコードを取得
        /// </summary>
        /// <returns>ハッシュコード</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }

        /// <summary>
        /// 等価演算子
        /// </summary>
        /// <param name="left">左辺のColorValue</param>
        /// <param name="right">右辺のColorValue</param>
        public static bool operator ==(ColorValue left, ColorValue right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等価演算子
        /// </summary>
        /// <param name="left">左辺のColorValue</param>
        /// <param name="right">右辺のColorValue</param>
        public static bool operator !=(ColorValue left, ColorValue right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 文字列形式の表示
        /// </summary>
        /// <returns>文字列形式</returns>
        public override string ToString()
        {
            return $"({R:F3}, {G:F3}, {B:F3}, {A:F3})";
        }
    }
}
