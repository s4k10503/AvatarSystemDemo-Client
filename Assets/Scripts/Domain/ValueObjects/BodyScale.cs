using System;

namespace Domain.ValueObjects
{
    /// <summary>
    /// ボディスケール
    /// </summary>
    [Serializable]
    public readonly struct BodyScale
    {
        public float Height { get; }      // 身長
        public float ShoulderWidth { get; }  // 肩幅
        public float BodyWidth { get; }   // 体の横幅
        public float HeadSize { get; }    // 頭の大きさ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="height">身長</param>
        /// <param name="shoulderWidth">肩幅</param>
        /// <param name="bodyWidth">体の横幅</param>
        public BodyScale(float height = 1f, float shoulderWidth = 1f, float bodyWidth = 1f, float headSize = 1f)
        {
            Height = Math.Clamp(height, 0.8f, 1.2f);           // 身長は±20%まで
            ShoulderWidth = Math.Clamp(shoulderWidth, 0.8f, 1.2f);  // 肩幅は±20%まで
            BodyWidth = Math.Clamp(bodyWidth, 0.8f, 1.2f);     // 体幅は±20%まで
            HeadSize = Math.Clamp(headSize, 0.7f, 1.3f);       // 頭の大きさは±30%まで
        }
    }
}
