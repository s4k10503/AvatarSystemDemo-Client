//
//SpingManager.cs for unity-chan!
//
//Original Script is here:
//ricopin / SpingManager.cs
//Rocket Jump : http://rocketjump.skr.jp/unity3d/109/
//https://twitter.com/ricopin416
//
//Revised by N.Kobayashi 2014/06/24
//           Y.Ebata
//
//Refactored by s4k10503 to improve performance and maintainability:

using UnityEngine;
using System;

namespace UnityChan
{
    public class SpringManager : MonoBehaviour
    {
        private Keyframe[] stiffnessKeys;
        private Keyframe[] dragKeys;
        private Action<SpringBone, float> stiffnessForceSetter;
        private Action<SpringBone, float> dragForceSetter;

        private const float MAX_DYNAMIC_RATIO = 1.0f;
        private const float MIN_DYNAMIC_RATIO = 0.0f;
        private const float ZERO_THRESHOLD = 0.0f;

        // 動的アニメーションのレベルを制御するパラメータ
        public float dynamicRatio = MAX_DYNAMIC_RATIO;
        public float stiffnessForce;
        public AnimationCurve stiffnessCurve;
        public float dragForce;
        public AnimationCurve dragCurve;
        public SpringBone[] springBones;

        /// <summary>
        /// 初期化
        /// </summary>
        private void Awake()
        {
            if (springBones == null || springBones.Length == 0)
            {
                Debug.LogError("SpringBonesが設定されていません");
                return;
            }

            if (stiffnessCurve == null || dragCurve == null)
            {
                Debug.LogError("AnimationCurveが設定されていません");
                return;
            }

            // デリゲートの初期化
            stiffnessForceSetter = (bone, value) => bone.stiffnessForce = value;
            dragForceSetter = (bone, value) => bone.dragForce = value;

            // カーブのキーフレーム情報をキャッシュ
            stiffnessKeys = stiffnessCurve.keys;
            dragKeys = dragCurve.keys;

            // 初期パラメータの設定
            UpdateParameters();
        }

        /// <summary>
        /// 毎フレーム実行される
        /// </summary>
        private void Update()
        {
#if UNITY_EDITOR
            if (dynamicRatio >= MAX_DYNAMIC_RATIO)
                dynamicRatio = MAX_DYNAMIC_RATIO;

            else if (dynamicRatio <= MIN_DYNAMIC_RATIO)
                dynamicRatio = MIN_DYNAMIC_RATIO;

            UpdateParameters();
#endif
        }

        /// <summary>
        /// すべてのUpdate関数が呼ばれた後に実行される
        /// </summary>
        private void LateUpdate()
        {
            // 動的比率が0の場合は物理演算をスキップ
            if (dynamicRatio == ZERO_THRESHOLD) return;

            for (int i = 0; i < springBones.Length; i++)
            {
                if (springBones[i] != null && dynamicRatio > springBones[i].threshold)
                {
                    // スプリングボーンの物理演算を実行
                    springBones[i].UpdateSpring();
                }
            }
        }

        /// <summary>
        /// すべてのパラメータを更新する
        /// </summary>
        private void UpdateParameters()
        {
            UpdateParameter(stiffnessForce, stiffnessCurve, stiffnessKeys, stiffnessForceSetter);
            UpdateParameter(dragForce, dragCurve, dragKeys, dragForceSetter);
        }

        /// <summary>
        /// パラメータを更新する
        /// </summary>
        /// <param name="baseValue">基本値</param>
        /// <param name="curve">アニメーションカーブ</param>
        /// <param name="keys">カーブのキーフレーム</param>
        /// <param name="setter">値を設定するアクション</param>
        private void UpdateParameter(
            float baseValue,
            AnimationCurve curve,
            Keyframe[] keys,
            Action<SpringBone, float> setter)
        {
            if (keys == null || keys.Length == 0) return;

            // カーブの開始時間と終了時間を取得
            var start = keys[0].time;
            var end = keys[^1].time;
            var length = springBones.Length - 1;

            for (int i = 0; i < springBones.Length; i++)
            {
                // 各ボーンの設定が有効な場合のみ更新
                if (springBones[i] != null && !springBones[i].isUseEachBoneForceSettings)
                {
                    // アニメーションカーブの値を取得
                    var scale = curve.Evaluate(start + (end - start) * i / length);
                    // パラメータを更新
                    setter(springBones[i], baseValue * scale);
                }
            }
        }

        /// <summary>
        /// スクリプトが破棄されるときに呼び出される
        /// </summary>
        private void OnDestroy()
        {
            stiffnessKeys = null;
            dragKeys = null;
            springBones = null;
            stiffnessCurve = null;
            dragCurve = null;
        }
    }
}
