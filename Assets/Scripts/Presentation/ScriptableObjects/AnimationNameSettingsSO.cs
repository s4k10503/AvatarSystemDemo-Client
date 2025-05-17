using UnityEngine;

namespace Presentation.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AnimationNameSettings", menuName = "Settings/Animation Name Settings")]
    public class AnimationNameSettingsSO : ScriptableObject
    {
        [Header("Animation Parameters")]
        public string ParamSpeed = "Speed";
        public string ParamIsRunning = "IsRunning";
        public string ParamIsJumping = "IsJumping";

        [Header("Animation Names")]
        public string AnimIdle = "Idle";
        public string AnimWalkForward = "WALK00_F";
        public string AnimWalkBackward = "WalkBack";
        public string AnimWalkLeft = "WALK00_L";
        public string AnimWalkRight = "WALK00_R";
        public string AnimRunForward = "RUN00_F";
        public string AnimRunLeft = "RUN00_L";
        public string AnimRunRight = "RUN00_R";
        public string AnimJump = "Jump";
    }
}
