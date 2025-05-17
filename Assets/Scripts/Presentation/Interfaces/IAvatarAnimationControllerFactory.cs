using UnityEngine;
using Presentation.Interfaces;

namespace Presentation.Factories
{
    /// <summary>
    /// アバターアニメーションコントローラーファクトリーのインターフェース
    /// </summary>
    public interface IAvatarAnimationControllerFactory
    {
        IAvatarAnimationController Create(Animator animator);
    }
}
