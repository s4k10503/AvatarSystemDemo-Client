using UnityEngine;

namespace Presentation.Events
{
    public struct AvatarReadyEvent
    {
        public Animator AvatarAnimator { get; }
        public AvatarReadyEvent(Animator animator)
        {
            AvatarAnimator = animator;
        }
    }
}