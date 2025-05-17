using UnityEngine;

namespace Presentation.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AvatarAnimationSettings", menuName = "Settings/Avatar Animation Settings")]
    public class AvatarAnimationSettingsSO : ScriptableObject
    {
        [Header("Direction Settings")]
        [Tooltip("Forward direction angle in degrees.")]
        public float ForwardAngle = 0f;
        [Tooltip("Right direction angle in degrees.")]
        public float RightAngle = 90f;
        [Tooltip("Backward direction angle in degrees.")]
        public float BackAngle = 180f;
        [Tooltip("Left direction angle in degrees.")]
        public float LeftAngle = 270f;
        [Tooltip("Threshold angle for determining direction range.")]
        public float AngleThreshold = 45f;
    }
}
