using UnityEngine;

namespace Presentation.ScriptableObjects
{
    /// <summary>
    /// API設定
    /// </summary>
    /// <remarks>
    /// API設定
    /// </remarks>
    [CreateAssetMenu(fileName = "ApiSettings", menuName = "Settings/ApiSettings")]
    public sealed class ApiSettings : ScriptableObject
    {
        // Change fields to public or add getters
        [Header("API Base Settings")]
        [SerializeField] public string baseUrl;

        [Header("Request Settings")]
        [SerializeField] public int maxRetries = 3;
        [SerializeField] public float initialInterval = 2f;
        [SerializeField] public float timeoutSeconds = 10f;

        [Header("Version Settings")]
        [SerializeField] public string appVersion = "1.0.0";
        [SerializeField] public string masterDataVersion = "1.0.0";
    }
}
