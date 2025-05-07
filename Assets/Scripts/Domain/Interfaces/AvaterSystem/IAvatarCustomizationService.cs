using Domain.ValueObjects;

namespace Domain.Interfaces
{
    /// <summary>
    /// アバター体型カスタマイズサービスのインターフェース
    /// </summary>
    public interface IAvatarCustomizationService
    {
        void ApplyBodyScale(BodyScale scale);
        void ApplyHeight(float height);
        void ApplyShoulderWidth(float width);
        void ApplyBodyWidth(float width);
        void ApplyHeadScale(float headSize);
        BodyScale GetCurrentBodyScale();
        void ResetBodyScale();
    }
}
