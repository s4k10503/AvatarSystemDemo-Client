//
//AutoBlink.cs
//2014/06/23 N.Kobayashi
//
//Refactored by s4k10503 to improve performance and maintainability:

using UnityEngine;
using System.Collections;

namespace UnityChan
{
    public class AutoBlink : MonoBehaviour
    {
        private const float DEFAULT_BLINK_TIME = 0.4f;
        private const float DEFAULT_INTERVAL = 3.0f;
        private const float DEFAULT_THRESHOLD = 0.3f;
        private const float DEFAULT_RATIO_CLOSE = 85.0f;
        private const float DEFAULT_RATIO_HALF_CLOSE = 20.0f;
        private const float DEFAULT_RATIO_OPEN = 0.0f;
        private const float HALF_CLOSE_THRESHOLD = 0.3f;
        private const int BLEND_SHAPE_INDEX = 6;

        [SerializeField] private bool isActive = true; // Enable Autoblink
        [SerializeField] private SkinnedMeshRenderer ref_SMR_EYE_DEF; // Reference to Eye
        [SerializeField] private SkinnedMeshRenderer ref_SMR_EL_DEF; // Reference to Eyelid
        [SerializeField] private float ratio_Close = DEFAULT_RATIO_CLOSE; // Close eye blend shape ratio
        [SerializeField] private float ratio_HalfClose = DEFAULT_RATIO_HALF_CLOSE; // Half-close eye blend shape ratio
        [SerializeField] private float timeBlink = DEFAULT_BLINK_TIME; // Blink length
        [SerializeField] private float threshold = DEFAULT_THRESHOLD; // Threshold for blink
        [SerializeField] private float interval = DEFAULT_INTERVAL; // Interval for blink

        [HideInInspector] public float ratio_Open = DEFAULT_RATIO_OPEN; // Open eye blend shape ratio

        private bool timerStarted = false;
        private bool isBlink = false;
        private float timeRemining = 0.0f;
        private Status eyeStatus;
        private Coroutine randomChangeCoroutine;

        private enum Status
        {
            Close,
            HalfClose,
            Open
        }

        /// <summary>
        /// スクリプトが有効になったときに呼び出される
        /// </summary>
        void Start()
        {
            ValidateReferences();
            ResetTimer();
            StartRandomBlink();
        }

        /// <summary>
        /// 参照が設定されていない場合はエラーメッセージを表示し、スクリプトを無効にする
        /// </summary>
        private void ValidateReferences()
        {
            if (ref_SMR_EYE_DEF == null || ref_SMR_EL_DEF == null)
            {
                Debug.LogError("SkinnedMeshRendererの参照が設定されていません");
                enabled = false;
            }
        }

        /// <summary>
        /// タイマーをリセットする
        /// </summary>
        void ResetTimer()
        {
            timeRemining = timeBlink;
            timerStarted = false;
        }

        /// <summary>
        /// 毎フレーム呼び出される
        /// </summary>
        void Update()
        {
            if (!isActive) return;

            if (!timerStarted)
            {
                eyeStatus = Status.Close;
                timerStarted = true;
            }
            if (timerStarted)
            {
                UpdateBlinkTimer();
            }
        }

        /// <summary>
        /// ブリンクタイマーを更新する
        /// </summary>
        private void UpdateBlinkTimer()
        {
            timeRemining -= Time.deltaTime;
            if (timeRemining <= 0.0f)
            {
                eyeStatus = Status.Open;
                ResetTimer();
            }
            else if (timeRemining <= timeBlink * HALF_CLOSE_THRESHOLD)
            {
                eyeStatus = Status.HalfClose;
            }
        }

        /// <summary>
        /// 毎フレーム呼び出される
        /// </summary>
        void LateUpdate()
        {
            if (!isActive || !isBlink) return;

            UpdateEyeBlendShape();
        }

        /// <summary>
        /// 目のブレンドシェイプを更新する
        /// </summary>
        private void UpdateEyeBlendShape()
        {
            switch (eyeStatus)
            {
                case Status.Close:
                    SetBlendShapeWeight(ratio_Close);
                    break;
                case Status.HalfClose:
                    SetBlendShapeWeight(ratio_HalfClose);
                    break;
                case Status.Open:
                    SetBlendShapeWeight(ratio_Open);
                    isBlink = false;
                    break;
            }
        }

        /// <summary>
        /// ブレンドシェイプの重みを設定する
        /// </summary>
        /// <param name="weight">重み</param>
        private void SetBlendShapeWeight(float weight)
        {
            if (ref_SMR_EYE_DEF != null)
                ref_SMR_EYE_DEF.SetBlendShapeWeight(BLEND_SHAPE_INDEX, weight);
            if (ref_SMR_EL_DEF != null)
                ref_SMR_EL_DEF.SetBlendShapeWeight(BLEND_SHAPE_INDEX, weight);
        }

        /// <summary>
        /// ランダムにブリンクを開始する
        /// </summary>
        private void StartRandomBlink()
        {
            if (randomChangeCoroutine != null)
            {
                StopCoroutine(randomChangeCoroutine);
            }
            randomChangeCoroutine = StartCoroutine(RandomChange());
        }

        /// <summary>
        /// ランダムにブリンクを開始する
        /// </summary>
        private IEnumerator RandomChange()
        {
            while (true)
            {
                if (!isBlink && Random.value > threshold)
                {
                    isBlink = true;
                }
                yield return new WaitForSeconds(interval);
            }
        }

        /// <summary>
        /// スクリプトが無効になったときに呼び出される
        /// </summary>
        private void OnDisable()
        {
            if (randomChangeCoroutine != null)
            {
                StopCoroutine(randomChangeCoroutine);
                randomChangeCoroutine = null;
            }
        }

        /// <summary>
        /// スクリプトが破棄されるときに呼び出される
        /// </summary>
        private void OnDestroy()
        {
            if (randomChangeCoroutine != null)
            {
                StopCoroutine(randomChangeCoroutine);
                randomChangeCoroutine = null;
            }
            ref_SMR_EYE_DEF = null;
            ref_SMR_EL_DEF = null;
        }
    }
}
