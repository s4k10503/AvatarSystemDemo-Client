using System;


namespace Domain.ValueObjects
{
    /// <summary>
    /// 髪の色を表す値オブジェクト
    /// </summary>
    [Serializable]
    public readonly struct HairColor : IEquatable<HairColor>
    {
        public readonly ColorValue Value; // UnityEngine.Color -> ColorValue

        // プリセットの定義 (彩度制限済みの値を使用)
        public static readonly HairColor Black = new(new ColorValue(0.1f, 0.1f, 0.1f));
        public static readonly HairColor DarkBrown = new(new ColorValue(0.25f, 0.17f, 0.15f));
        public static readonly HairColor Brown = new(new ColorValue(0.4f, 0.26f, 0.24f));
        public static readonly HairColor Blonde = new(new ColorValue(0.85f, 0.714f, 0.51f));
        public static readonly HairColor White = new(new ColorValue(1f, 1f, 1f));
        public static readonly HairColor Red = new(new ColorValue(0.7f, 0.42f, 0.42f));
        public static readonly HairColor Blue = new(new ColorValue(0.42f, 0.42f, 0.7f));

        // デフォルト値 (Whiteを参照)
        public static readonly HairColor Default = White;

        // プリセットリスト (UI表示用)
        private static readonly HairColor[] _presets = {
            Black, DarkBrown, Brown, Blonde, White, Red, Blue
        };


        // コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="value">色の値</param>
        public HairColor(ColorValue value)
        {
            Value = value; // 直接代入に戻す
        }

        // float を受け取るコンストラクタ
        /// <summary>
        /// float を受け取るコンストラクタ
        /// </summary>
        /// <param name="r">赤</param>
        /// <param name="g">緑</param>
        /// <param name="b">青</param>
        public HairColor(float r, float g, float b, float a = 1.0f)
            : this(new ColorValue(r, g, b, a))
        {
        }

        /// <summary>
        /// IEquatableインターフェースの実装
        /// </summary>
        /// <param name="other">比較対象のHairColor</param>
        /// <returns>等しい場合はtrue、それ以外はfalse</returns>
        public bool Equals(HairColor other)
        {
            return Value.Equals(other.Value);
        }

        // オブジェクトの等価性を確認
        /// <summary>
        /// オブジェクトの等価性を確認
        /// </summary>
        /// <param name="obj">比較対象のオブジェクト</param>
        /// <returns>等しい場合はtrue、それ以外はfalse</returns>
        public override bool Equals(object obj)
        {
            return obj is HairColor other && Equals(other);
        }

        // ハッシュコードを取得
        /// <summary>
        /// ハッシュコードを取得
        /// </summary>
        /// <returns>ハッシュコード</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        // 等価演算子
        /// <summary>
        /// 等価演算子
        /// </summary>
        /// <param name="left">左辺のHairColor</param>
        /// <param name="right">右辺のHairColor</param>
        public static bool operator ==(HairColor left, HairColor right)
        {
            return left.Equals(right);
        }

        // 不等価演算子
        /// <summary>
        /// 不等価演算子
        /// </summary>
        /// <param name="left">左辺のHairColor</param>
        /// <param name="right">右辺のHairColor</param>
        public static bool operator !=(HairColor left, HairColor right)
        {
            return !(left == right);
        }

        /// <summary>
        /// プリセット取得メソッド
        /// </summary>
        /// <returns>プリセット</returns>
        public static HairColor[] GetPresets()
        {
            return _presets;
        }

        /// <summary>
        /// 文字列形式の表示
        /// </summary>
        /// <returns>文字列形式</returns>
        public override string ToString()
        {
            // Use ColorValue's ToString()
            return $"HairColor({Value})";
        }
    }
}
