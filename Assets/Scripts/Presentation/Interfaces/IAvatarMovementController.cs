using UnityEngine;

namespace Presentation.Interfaces
{
    /// <summary>
    /// アバターの移動制御インターフェース
    /// </summary>
    public interface IAvatarMovementController
    {
        /// <summary>
        /// 移動方向を設定
        /// </summary>
        void SetMoveDirection(Vector2 direction);

        /// <summary>
        /// ジャンプを設定
        /// </summary>
        void SetJump();

        /// <summary>
        /// 移動を停止
        /// </summary>
        void StopMovement();

        /// <summary>
        /// 現在の移動状態を取得
        /// </summary>
        AvatarMovementState GetMovementState();

        /// <summary>
        /// アニメーションコントローラを設定
        /// </summary>
        /// <param name="animationController">アバター用アニメーションコントローラ</param>
        void SetAnimationController(IAvatarAnimationController animationController);
    }

    /// <summary>
    /// アバターの移動状態
    /// </summary>
    public readonly struct AvatarMovementState
    {
        public readonly Vector3 MoveDirection { get; }
        public readonly float CurrentSpeed { get; }
        public readonly bool IsRunning { get; }
        public readonly bool IsJumping { get; }
        public readonly bool IsGrounded { get; }

        public AvatarMovementState(
            Vector3 moveDirection,
            float currentSpeed,
            bool isRunning,
            bool isJumping,
            bool isGrounded)
        {
            MoveDirection = moveDirection;
            CurrentSpeed = currentSpeed;
            IsRunning = isRunning;
            IsJumping = isJumping;
            IsGrounded = isGrounded;
        }
    }
}
