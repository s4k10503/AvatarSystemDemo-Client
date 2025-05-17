using UnityEngine;
using Presentation.Interfaces;
using Presentation.View; // For AvatarAnimationController concrete class
using Presentation.ScriptableObjects;
using VContainer;

namespace Presentation.Factories
{
    /// <summary>
    /// アバターアニメーションコントローラーファクトリーの実装
    /// </summary>
    public class AvatarAnimationControllerFactory : IAvatarAnimationControllerFactory
    {
        private readonly AvatarAnimationSettingsSO _directionSettings;
        private readonly AnimationNameSettingsSO _nameSettings;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="directionSettings">アニメーション方向設定</param>
        /// <param name="nameSettings">アニメーション名設定</param>
        [Inject]
        public AvatarAnimationControllerFactory(
            AvatarAnimationSettingsSO directionSettings,
            AnimationNameSettingsSO nameSettings)
        {
            _directionSettings = directionSettings;
            _nameSettings = nameSettings;
        }

        /// <summary>
        /// アバターアニメーションコントローラーを作成
        /// </summary>
        /// <param name="animator">アニメーター</param>
        /// <returns>アバターアニメーションコントローラー</returns>
        public IAvatarAnimationController Create(Animator animator)
        {
            if (animator == null)
            {
                Debug.LogError("[AvatarAnimationControllerFactory] Animatorが nullのため、AvatarAnimationControllerを作成できません。");
                return null;
            }
            if (_directionSettings == null)
            {
                Debug.LogError("[AvatarAnimationControllerFactory] AvatarAnimationSettingsSOがnullのため、AvatarAnimationControllerを作成できません。");
                return null;
            }
            if (_nameSettings == null)
            {
                Debug.LogError("[AvatarAnimationControllerFactory] AnimationNameSettingsSOがnullのため、AvatarAnimationControllerを作成できません。");
                return null;
            }
            return new AvatarAnimationController(animator, _directionSettings, _nameSettings);
        }
    }
}
