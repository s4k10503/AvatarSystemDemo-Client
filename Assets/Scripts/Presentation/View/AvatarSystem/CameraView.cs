using UnityEngine;

using Application.DTO;
using Presentation.Interfaces;

namespace Presentation.View
{
    /// <summary>
    /// アバターカスタマイズ用のカメラコントローラー
    /// </summary>
    public sealed class CameraView : MonoBehaviour, ICameraView
    {
        #region シリアライズフィールド
        [Header("共通設定")]
        [SerializeField] private float rotationSpeed = 0.5f; // 回転速度係数

        [Header("顔モード設定")]
        [SerializeField] private float faceModeDistanceRatio = 0.3f;   // 初期距離に対する顔モード距離の比率
        [SerializeField] private float faceModeHeightOffset = -0.1f;   // 顔の位置からのカメラ高さオフセット
        [SerializeField] private float faceModeSizeAdjustRatio = 0.2f; // 顔サイズに応じた距離調整の比率
        [SerializeField] private float faceModeSizeBase = 2.0f;        // 顔サイズ調整の基準値

        [Header("ズーム設定")] // Zoom settings
        [SerializeField] private float zoomSpeed = 0.01f; // ズーム速度
        [SerializeField] private float minZoomDistanceRatio = 0.5f; // 最小ズーム距離の初期距離に対する比率
        [SerializeField] private float maxZoomDistanceRatio = 2.0f; // 最大ズーム距離の初期距離に対する比率

        [Header("高さ調整設定")] // Height adjustment settings
        [SerializeField] private float heightAdjustSpeed = 0.01f; // 高さ調整速度
        [SerializeField] private float minHeightOffsetRatio = -0.5f; // 初期高さに対する最小オフセット比率
        [SerializeField] private float maxHeightOffsetRatio = 0.5f;  // 初期高さに対する最大オフセット比率
        #endregion

        #region プライベートフィールド
        private Transform _target;
        private float _initialDistance;
        private float _currentDistance; // 現在のズームレベルでの距離
        private Vector3 _initialPosition;
        private Vector3 _initialOffset;
        private float _currentRotationAngle;
        private Quaternion _initialRotation;

        // 顔モード関連
        private bool _isFaceMode;
        private Vector3 _headPosition;  // 外部から設定される顔の位置（ワールド座標）
        private Vector3 _headOffset;    // 外部から設定される顔のオフセット（アバタールートからの相対位置）
        private float _faceModeDistance;
        private float _currentFaceModeDistance; // 顔モードでの現在のズームレベルでの距離

        // 高さ調整関連
        private float _initialCameraHeight;
        private float _currentHeightOffset; // 初期高さからの現在のオフセット
        #endregion

        #region 初期化処理
        private void Awake()
        {
            // 初期位置と回転を記録
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;

            // カメラの初期高さを記録
            _initialCameraHeight = _initialPosition.y;
            // 高さオフセットを初期化
            _currentHeightOffset = 0f;
            // 頭部のデフォルトオフセットを初期化
            _headOffset = new Vector3(0, 1.0f, 0);
        }
        #endregion

        #region パブリックメソッド
        /// <summary>
        /// ターゲットを設定
        /// </summary>
        /// <param name="newTarget">新しいターゲット</param>
        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            if (_target == null) return;

            // ターゲット基準の初期状態を保存
            _initialOffset = _initialPosition - _target.position;
            _initialDistance = new Vector2(_initialOffset.x, _initialOffset.z).magnitude;
            _currentDistance = _initialDistance;
            _currentRotationAngle = 0f;

            // 顔モード用の初期値を設定
            _faceModeDistance = _initialDistance * faceModeDistanceRatio;
            _currentFaceModeDistance = _faceModeDistance;

            // 頭の位置を計算
            _headPosition = _target.position + _headOffset;

            // ターゲット変更時に高さオフセットをリセット
            _currentHeightOffset = 0f;
        }

        /// <summary>
        /// 顔の位置情報を設定
        /// </summary>
        public void SetHeadPosition(Vector3 headOffset, Vector3 headPosition)
        {
            _headOffset = headOffset;
            _headPosition = headPosition;

            // 顔モード中なら即座にカメラ位置を更新
            if (_isFaceMode)
            {
                UpdateCameraPosition();
            }
        }

        /// <summary>
        /// アバター設定値を更新
        /// </summary>
        public void UpdateAvatarSettings(AvatarSettingsViewModel settings)
        {
            if (_target == null || settings == null) return;

            // 顔モード用のカメラ距離を設定（顔の大きさに応じて調整）
            float baseFaceDistance = _initialDistance * faceModeDistanceRatio; // Recalculate base
            float headSizeFactor = faceModeSizeAdjustRatio * (faceModeSizeBase - settings.HeadSize);
            // Preserve current zoom level relative to the new base distance influenced by head size.
            float zoomRatio = _currentFaceModeDistance / _faceModeDistance;
            _faceModeDistance = baseFaceDistance * headSizeFactor; // Update base distance for face mode
            _currentFaceModeDistance = _faceModeDistance * zoomRatio; // Apply current zoom ratio to new base
            _currentFaceModeDistance = Mathf.Clamp(_currentFaceModeDistance, _initialDistance * faceModeDistanceRatio * minZoomDistanceRatio, _initialDistance * faceModeDistanceRatio * maxZoomDistanceRatio);

            // 設定変更後にカメラ位置を更新
            if (_isFaceMode)
            {
                UpdateCameraPosition();
            }
        }

        /// <summary>
        /// 回転角度を設定
        /// </summary>
        public void SetRotationAngle(float rotationAngle)
        {
            _currentRotationAngle = rotationAngle;
            UpdateCameraPosition();
        }

        /// <summary>
        /// 回転角度を変化量で更新
        /// </summary>
        public void UpdateRotationByDelta(Vector2 delta)
        {
            _currentRotationAngle += delta.x * rotationSpeed;
            UpdateCameraPosition();
        }

        /// <summary>
        /// 現在の回転角度を取得
        /// </summary>
        public float GetCurrentRotationAngle()
        {
            return _currentRotationAngle;
        }

        /// <summary>
        /// 顔モードを設定
        /// </summary>
        public void SetFaceMode(bool isFaceMode)
        {
            if (_isFaceMode == isFaceMode) return;

            _isFaceMode = isFaceMode;
            _currentRotationAngle = 0f; // 角度をリセット
            _currentHeightOffset = 0f; // 高さオフセットをリセット

            // カメラ位置を更新
            UpdateCameraPosition();
        }

        /// <summary>
        /// ピンチ操作によるズームを調整
        /// </summary>
        /// <param name="pinchDelta">ピンチの距離の変化量</param>
        public void AdjustZoom(float pinchDelta)
        {
            if (_target == null)
            {
                Debug.LogWarning("[CameraView] ターゲットが設定されていないため、ズームを調整できません。");
                return;
            }

            float zoomAmount = pinchDelta * zoomSpeed;

            if (_isFaceMode)
            {
                // 顔モードでは、顔モード用の距離を調整
                _currentFaceModeDistance -= zoomAmount; // Zoom in makes distance smaller
                // Clamp face mode distance (using initial distance and face mode ratio as base for min/max)
                float minFaceZoom = _initialDistance * faceModeDistanceRatio * minZoomDistanceRatio;
                float maxFaceZoom = _initialDistance * faceModeDistanceRatio * maxZoomDistanceRatio;
                _currentFaceModeDistance = Mathf.Clamp(_currentFaceModeDistance, minFaceZoom, maxFaceZoom);
            }
            else
            {
                // ボディモードでは、基本距離を調整
                _currentDistance -= zoomAmount; // Zoom in makes distance smaller
                // Clamp body mode distance
                _currentDistance = Mathf.Clamp(_currentDistance, _initialDistance * minZoomDistanceRatio, _initialDistance * maxZoomDistanceRatio);
            }
            UpdateCameraPosition();
        }

        /// <summary>
        /// ドラッグ操作によるカメラの高さを調整
        /// </summary>
        /// <param name="heightDelta">高さの変化量</param>
        public void AdjustHeight(float heightDelta)
        {
            if (_target == null)
            {
                Debug.LogWarning("[CameraView] ターゲットが設定されていないため、高さを調整できません。");
                return;
            }

            _currentHeightOffset += heightDelta * heightAdjustSpeed;

            // 高さを制限
            float minHeight = _initialCameraHeight + _initialDistance * minHeightOffsetRatio;
            float maxHeight = _initialCameraHeight + _initialDistance * maxHeightOffsetRatio;
            // オフセットではなく、実際のY座標でClampする方が直感的かもしれない。
            // 現在の実装では、初期のターゲットからの相対的な高さオフセットを保持し、
            // ターゲットのY座標が変わった場合にも対応できるようにしている。
            // ここでは、カメラの絶対的なY座標が初期高さ ± (初期距離 * 比率) の範囲になるようにオフセットをクランプする。
            // このため、ターゲットのY座標が大きく変わる場合は、min/maxHeightOffsetRatioの再考が必要。

            // 現在のカメラのY座標を計算
            float currentAbsoluteY = _initialCameraHeight + _currentHeightOffset;
            currentAbsoluteY = Mathf.Clamp(currentAbsoluteY, minHeight, maxHeight);
            _currentHeightOffset = currentAbsoluteY - _initialCameraHeight;

            UpdateCameraPosition();
        }
        #endregion

        #region プライベートメソッド
        /// <summary>
        /// カメラの位置を更新
        /// </summary>
        private void UpdateCameraPosition()
        {
            if (_target == null) return;

            if (_isFaceMode)
            {
                UpdateFaceModeCamera();
            }
            else
            {
                UpdateBodyModeCamera();
            }
        }

        /// <summary>
        /// 顔モード時のカメラ位置を更新
        /// </summary>
        private void UpdateFaceModeCamera()
        {
            // 顔の位置を使用
            Vector3 targetPoint = _headPosition;
            float distance = _currentFaceModeDistance; // Use current zoomed distance

            // 回転角度から顔の周囲の位置を計算
            float angleRad = _currentRotationAngle * Mathf.Deg2Rad;
            float x = targetPoint.x + distance * Mathf.Sin(angleRad);
            float z = targetPoint.z + distance * Mathf.Cos(angleRad);

            // 高さは顔の位置からオフセット + ユーザーによる高さ調整
            float y = targetPoint.y + faceModeHeightOffset + _currentHeightOffset;

            // 新しいカメラ位置を設定
            Vector3 newPosition = new(x, y, z);
            transform.position = newPosition;

            // 顔を見るように回転
            transform.LookAt(targetPoint);

            // 上下角度を初期状態に近づける
            AdjustVerticalRotation();
        }

        /// <summary>
        /// 体型モード時のカメラ位置を更新
        /// </summary>
        private void UpdateBodyModeCamera()
        {
            // 水平回転を適用
            Vector3 targetPoint = _target.position;

            // 水平面での回転移動を計算
            float angleRad = _currentRotationAngle * Mathf.Deg2Rad;
            Vector3 directionXZ = new(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));

            // 新しい位置を設定（Y座標は初期位置と同じ + ユーザーによる高さ調整）
            Vector3 newPosition = targetPoint + directionXZ * _currentDistance; // Use current zoomed distance
            newPosition.y = _initialCameraHeight + _currentHeightOffset;

            transform.position = newPosition;

            // 水平方向のみ回転
            ApplyHorizontalRotation();
        }

        /// <summary>
        /// カメラの上下回転角度を調整
        /// </summary>
        private void AdjustVerticalRotation()
        {
            Vector3 currentEuler = transform.rotation.eulerAngles;
            Quaternion adjustedRotation = Quaternion.Euler(
                _initialRotation.eulerAngles.x, // 上下角度は初期値
                currentEuler.y,                 // 水平方向はLookAtで決定された値
                _initialRotation.eulerAngles.z  // 傾きは初期値
            );
            transform.rotation = adjustedRotation;
        }

        /// <summary>
        /// 水平方向のみの回転を適用
        /// </summary>
        private void ApplyHorizontalRotation()
        {
            Quaternion horizontalRotation = Quaternion.Euler(
                _initialRotation.eulerAngles.x,                 // 上下方向の角度は初期値
                _initialRotation.eulerAngles.y + _currentRotationAngle,  // 水平方向のみ回転
                _initialRotation.eulerAngles.z                  // 傾きは初期値
            );

            transform.rotation = horizontalRotation;
        }
        #endregion
    }
}
