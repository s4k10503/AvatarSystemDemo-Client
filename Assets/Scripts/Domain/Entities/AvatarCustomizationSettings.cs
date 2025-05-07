using System;
using Domain.ValueObjects;

namespace Domain.Entities
{
    /// <summary>
    /// アバターカスタマイズ設定を表すエンティティクラス
    /// </summary>
    [Serializable]
    public class AvatarCustomizationSettings
    {
        /// <summary>
        /// エンティティの識別子
        /// </summary>
        public string id;
        public float Height { get; set; }
        public float ShoulderWidth { get; set; }
        public float BodyWidth { get; set; }
        public float HeadSize { get; set; }

        // Color values are stored as floats, which is framework independent.
        public float SkinColorR { get; set; }
        public float SkinColorG { get; set; }
        public float SkinColorB { get; set; }
        public float SkinColorA { get; set; }

        public float HairColorR { get; set; }
        public float HairColorG { get; set; }
        public float HairColorB { get; set; }
        public float HairColorA { get; set; }

        /// <summary>
        /// 新しい設定インスタンスを作成します
        /// </summary>
        public AvatarCustomizationSettings()
        {
            // デフォルトコンストラクタではプロパティの初期値が適用
            Height = 1.0f;
            ShoulderWidth = 1.0f;
            BodyWidth = 1.0f;
            HeadSize = 1.0f;

            // Initialize from default Value Objects
            var defaultSkinColor = SkinColor.Default;
            SkinColorR = defaultSkinColor.Value.R;
            SkinColorG = defaultSkinColor.Value.G;
            SkinColorB = defaultSkinColor.Value.B;
            SkinColorA = defaultSkinColor.Value.A;

            var defaultHairColor = HairColor.Default;
            HairColorR = defaultHairColor.Value.R;
            HairColorG = defaultHairColor.Value.G;
            HairColorB = defaultHairColor.Value.B;
            HairColorA = defaultHairColor.Value.A;
        }

        /// <summary>
        /// 値を指定して新しい設定インスタンスを作成
        /// </summary>
        public AvatarCustomizationSettings(
            string id,
            float height,
            float shoulderWidth,
            float bodyWidth,
            float headSize,
            SkinColor skinColor,
            HairColor hairColor
        )
        {
            this.id = id;
            Height = height;
            ShoulderWidth = shoulderWidth;
            BodyWidth = bodyWidth;
            HeadSize = headSize;
            // Assign from Value Objects
            SkinColorR = skinColor.Value.R;
            SkinColorG = skinColor.Value.G;
            SkinColorB = skinColor.Value.B;
            SkinColorA = skinColor.Value.A;
            HairColorR = hairColor.Value.R;
            HairColorG = hairColor.Value.G;
            HairColorB = hairColor.Value.B;
            HairColorA = hairColor.Value.A;
        }

        /// <summary>
        /// 値を指定して新しい設定インスタンスを作成 (IDなし)
        /// </summary>
        public AvatarCustomizationSettings(
            float height,
            float shoulderWidth,
            float bodyWidth,
            float headSize,
            SkinColor skinColor,
            HairColor hairColor
        )
            : this(string.Empty, height, shoulderWidth, bodyWidth, headSize, skinColor, hairColor)
        {
        }

        /// <summary>
        /// 後方互換性のための古いコンストラクタ (髪色をデフォルトで追加)
        /// </summary>
        public AvatarCustomizationSettings(
            string id,
            float height,
            float shoulderWidth,
            float bodyWidth,
            float headSize,
            SkinColor skinColor
        )
            : this(id, height, shoulderWidth, bodyWidth, headSize, skinColor, HairColor.Default)
        {
        }

        /// <summary>
        /// 後方互換性のための古いコンストラクタ (髪色なし、IDなし)
        /// </summary>
        public AvatarCustomizationSettings(
            float height,
            float shoulderWidth,
            float bodyWidth,
            float headSize
        )
            : this(string.Empty, height, shoulderWidth, bodyWidth, headSize, SkinColor.Default, HairColor.Default)
        {
        }

        /// <summary>
        /// 設定値のクローンを作成
        /// </summary>
        public AvatarCustomizationSettings Clone()
        {
            return new AvatarCustomizationSettings(
                this.id,
                this.Height,
                this.ShoulderWidth,
                this.BodyWidth,
                this.HeadSize,
                this.ToSkinColor(),
                this.ToHairColor()
            );
        }

        /// <summary>
        /// 設定から肌色情報を取得する
        /// </summary>
        public SkinColor ToSkinColor()
        {
            // Create SkinColor using ColorValue
            return new SkinColor(new ColorValue(SkinColorR, SkinColorG, SkinColorB, SkinColorA));
        }

        /// <summary>
        /// 肌色設定を更新する
        /// </summary>
        public void UpdateSkinColor(SkinColor skinColor)
        {
            // Update from SkinColor's Value
            SkinColorR = skinColor.Value.R;
            SkinColorG = skinColor.Value.G;
            SkinColorB = skinColor.Value.B;
            SkinColorA = skinColor.Value.A;
        }

        /// <summary>
        /// 設定から髪色情報を取得する
        /// </summary>
        public HairColor ToHairColor()
        {
            // Create HairColor using ColorValue
            return new HairColor(new ColorValue(HairColorR, HairColorG, HairColorB, HairColorA));
        }

        /// <summary>
        /// 髪色設定を更新する
        /// </summary>
        public void UpdateHairColor(HairColor hairColor)
        {
            // Update from HairColor's Value
            HairColorR = hairColor.Value.R;
            HairColorG = hairColor.Value.G;
            HairColorB = hairColor.Value.B;
            HairColorA = hairColor.Value.A;
        }

        /// <summary>
        /// 別のAvatarCustomizationSettingsと値を比較して同じかどうか判断
        /// 注意: これは値の比較であり、エンティティの同一性の比較ではない
        /// </summary>
        /// <param name="other">比較対象</param>
        /// <param name="epsilon">許容誤差</param>
        /// <returns>すべての値が一致していればtrue</returns>
        public bool EqualsWithTolerance(AvatarCustomizationSettings other, float epsilon = 0.001f)
        {
            if (other == null) return false;

            return IsApproximatelyEqual(Height, other.Height, epsilon) &&
                   IsApproximatelyEqual(ShoulderWidth, other.ShoulderWidth, epsilon) &&
                   IsApproximatelyEqual(BodyWidth, other.BodyWidth, epsilon) &&
                   IsApproximatelyEqual(HeadSize, other.HeadSize, epsilon) &&
                   IsApproximatelyEqual(SkinColorR, other.SkinColorR, epsilon) &&
                   IsApproximatelyEqual(SkinColorG, other.SkinColorG, epsilon) &&
                   IsApproximatelyEqual(SkinColorB, other.SkinColorB, epsilon) &&
                   IsApproximatelyEqual(SkinColorA, other.SkinColorA, epsilon) &&
                   IsApproximatelyEqual(HairColorR, other.HairColorR, epsilon) &&
                   IsApproximatelyEqual(HairColorG, other.HairColorG, epsilon) &&
                   IsApproximatelyEqual(HairColorB, other.HairColorB, epsilon) &&
                   IsApproximatelyEqual(HairColorA, other.HairColorA, epsilon);
        }

        /// <summary>
        /// 2つの浮動小数点値が指定された許容誤差内で等しいかどうか判断
        /// </summary>
        private static bool IsApproximatelyEqual(float a, float b, float epsilon)
        {
            return Math.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// 値をBodyScaleインスタンスに変換
        /// </summary>
        public BodyScale ToBodyScale()
        {
            return new BodyScale(Height, ShoulderWidth, BodyWidth, HeadSize);
        }

        /// <summary>
        /// BodyScaleからAvatarCustomizationSettingsを作成
        /// </summary>
        public static AvatarCustomizationSettings FromBodyScale(string id, BodyScale bodyScale, SkinColor skinColor, HairColor hairColor)
        {
            return new AvatarCustomizationSettings(id, bodyScale.Height, bodyScale.ShoulderWidth, bodyScale.BodyWidth, bodyScale.HeadSize, skinColor, hairColor);
        }

        /// <summary>
        /// BodyScaleからAvatarCustomizationSettingsを作成 (肌色指定あり、IDなし)
        /// </summary>
        public static AvatarCustomizationSettings FromBodyScale(BodyScale bodyScale, SkinColor skinColor, HairColor hairColor)
        {
            return FromBodyScale(string.Empty, bodyScale, skinColor, hairColor);
        }

        /// <summary>
        /// BodyScaleからAvatarCustomizationSettingsを作成 (肌色はデフォルト)
        /// </summary>
        public static AvatarCustomizationSettings FromBodyScale(string id, BodyScale bodyScale, SkinColor skinColor)
        {
            return FromBodyScale(id, bodyScale, skinColor, HairColor.Default);
        }

        /// <summary>
        /// BodyScaleからAvatarCustomizationSettingsを作成 (肌色はデフォルト、IDなし)
        /// </summary>
        public static AvatarCustomizationSettings FromBodyScale(BodyScale bodyScale, SkinColor skinColor)
        {
            return FromBodyScale(string.Empty, bodyScale, skinColor, HairColor.Default);
        }

        /// <summary>
        /// BodyScaleからAvatarCustomizationSettingsを作成 (肌色はデフォルト)
        /// </summary>
        public static AvatarCustomizationSettings FromBodyScale(string id, BodyScale bodyScale)
        {
            return FromBodyScale(id, bodyScale, SkinColor.Default, HairColor.Default);
        }

        /// <summary>
        /// BodyScaleからAvatarCustomizationSettingsを作成 (肌色はデフォルト、IDなし)
        /// </summary>
        public static AvatarCustomizationSettings FromBodyScale(BodyScale bodyScale)
        {
            return FromBodyScale(string.Empty, bodyScale, SkinColor.Default, HairColor.Default);
        }

        /// <summary>
        /// エンティティの等価性を評価
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((AvatarCustomizationSettings)obj);
        }

        /// <summary>
        /// エンティティの等価性を評価
        /// </summary>
        protected bool Equals(AvatarCustomizationSettings other)
        {
            // IDが存在しない場合はリファレンス等価性を使用
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(other.id))
                return ReferenceEquals(this, other);

            return string.Equals(id, other.id);
        }

        /// <summary>
        /// エンティティのハッシュコードを返す
        /// </summary>
        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(id) ? base.GetHashCode() : id.GetHashCode();
        }
    }
}
