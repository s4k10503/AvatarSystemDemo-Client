using UnityEngine;
using Presentation.Interfaces;
using Presentation.ScriptableObjects;

namespace Presentation.View
{
    /// <summary>
    /// アバターのアニメーション制御 (POCO)
    /// </summary>
    public sealed class AvatarAnimationController : IAvatarAnimationController
    {
        #region Readonly Fields
        private readonly Animator _animator;
        private readonly AvatarAnimationSettingsSO _animationDirectionSettings;
        private readonly AnimationNameSettingsSO _animationNameSettings;
        #endregion

        #region Private Fields
        private string _currentAnimationState;
        #endregion

        public AvatarAnimationController(Animator animator, AvatarAnimationSettingsSO directionSettings, AnimationNameSettingsSO nameSettings)
        {
            _animator = animator;
            _animationDirectionSettings = directionSettings;
            _animationNameSettings = nameSettings;

            if (_animator == null)
            {
                Debug.LogError("[AvatarAnimationController] Animator component is null!");
            }
            if (_animationDirectionSettings == null)
            {
                Debug.LogError("[AvatarAnimationController] AvatarAnimationSettingsSO is null!");
            }
            if (_animationNameSettings == null)
            {
                Debug.LogError("[AvatarAnimationController] AnimationNameSettingsSO is null!");
            }

            // Initialize current animation state using the SO
            _currentAnimationState = _animationNameSettings != null ? _animationNameSettings.AnimIdle : "WAIT00";
            PlayAnimation(_currentAnimationState);
        }

        #region Public Methods

        /// <summary>
        /// アニメーションを更新
        /// </summary>
        public void UpdateAnimation(AvatarMovementState movementState)
        {
            if (_animator == null || _animationDirectionSettings == null || _animationNameSettings == null) return;

            UpdateAnimationParameters(movementState);
            if (!movementState.IsJumping)
            {
                string targetAnimation = DetermineAnimationState(movementState);
                PlayAnimation(targetAnimation);
            }
            else
            {
                PlayAnimation(_animationNameSettings.AnimJump);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// アニメーションパラメータを更新
        /// </summary>
        private void UpdateAnimationParameters(AvatarMovementState movementState)
        {
            _animator.SetFloat(_animationNameSettings.ParamSpeed, movementState.CurrentSpeed);
            _animator.SetBool(_animationNameSettings.ParamIsRunning, movementState.IsRunning);
            _animator.SetBool(_animationNameSettings.ParamIsJumping, movementState.IsJumping);
        }

        /// <summary>
        /// 現在の状態に応じたアニメーションを決定
        /// </summary>
        private string DetermineAnimationState(AvatarMovementState movementState)
        {
            if (movementState.CurrentSpeed < 0.1f)
            {
                return _animationNameSettings.AnimIdle;
            }

            float angle = Mathf.Atan2(movementState.MoveDirection.x, movementState.MoveDirection.z) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            return movementState.IsRunning
                ? GetDirectionAnimation(angle, true)
                : GetDirectionAnimation(angle, false);
        }

        /// <summary>
        /// 方向に応じたアニメーションを取得
        /// </summary>
        private string GetDirectionAnimation(float angle, bool isRunning)
        {
            if (IsInDirectionRange(angle, _animationDirectionSettings.ForwardAngle))
            {
                return isRunning ? _animationNameSettings.AnimRunForward : _animationNameSettings.AnimWalkForward;
            }
            if (IsInDirectionRange(angle, _animationDirectionSettings.RightAngle))
            {
                return isRunning ? _animationNameSettings.AnimRunRight : _animationNameSettings.AnimWalkRight;
            }
            if (IsInDirectionRange(angle, _animationDirectionSettings.BackAngle))
            {
                return isRunning ? _animationNameSettings.AnimRunForward : _animationNameSettings.AnimWalkBackward;
            }
            return isRunning ? _animationNameSettings.AnimRunLeft : _animationNameSettings.AnimWalkLeft;
        }

        /// <summary>
        /// 指定された角度が基準角度の範囲内かどうかを判定
        /// </summary>
        private bool IsInDirectionRange(float angle, float baseAngle)
        {
            float minAngle = (baseAngle - _animationDirectionSettings.AngleThreshold + 360f) % 360f;
            float maxAngle = (baseAngle + _animationDirectionSettings.AngleThreshold) % 360f;

            if (minAngle > maxAngle)
            {
                return angle >= minAngle || angle <= maxAngle;
            }
            return angle >= minAngle && angle <= maxAngle;
        }

        /// <summary>
        /// アニメーションを再生
        /// </summary>
        private void PlayAnimation(string animationName)
        {
            if (_animator != null && _currentAnimationState != animationName && !string.IsNullOrEmpty(animationName))
            {
                _animator.Play(animationName);
                _currentAnimationState = animationName;
            }
        }

        #endregion
    }
}
