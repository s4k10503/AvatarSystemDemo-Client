namespace Presentation.Interfaces
{
    /// <summary>
    /// アバターのアニメーション制御インターフェース
    /// </summary>
    public interface IAvatarAnimationController
    {
        /// <summary>
        /// アニメーションを更新
        /// </summary>
        void UpdateAnimation(AvatarMovementState movementState);
    }
}
