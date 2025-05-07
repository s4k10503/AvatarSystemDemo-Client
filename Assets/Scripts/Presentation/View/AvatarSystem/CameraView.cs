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
        #endregion

        #region プライベートフィールド
        private Transform _target;
        private float _initialDistance;
        private Vector3 _initialPosition;
        private Vector3 _initialOffset;
        private float _currentRotationAngle;
        private Quaternion _initialRotation;

        // 顔モード関連
        private bool _isFaceMode;
        private Vector3 _headPosition;  // 外部から設定される顔の位置（ワールド座標）
        private Vector3 _headOffset;    // 外部から設定される顔のオフセット（アバタールートからの相対位置）
        private float _faceModeDistance;
        #endregion

        #region 初期化処理
        private void Awake()
        {
            // 初期位置と回転を記録
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }
        #endregion

        #region パブリックメソッド
        /// <summary>
        /// ターゲットを設定
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            if (_target == null) return;

            // 初期状態を保存
            _initialPosition = transform.position;
            _initialOffset = _initialPosition - _target.position;
            _initialDistance = new Vector2(_initialOffset.x, _initialOffset.z).magnitude;
            _currentRotationAngle = 0f;
            _initialRotation = transform.rotation;

            // 初期値を設定
            _faceModeDistance = _initialDistance * faceModeDistanceRatio;
            _headOffset = new Vector3(0, 1.0f, 0);
            _headPosition = _target.position + _headOffset;
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
            _faceModeDistance = _initialDistance * faceModeSizeAdjustRatio * (faceModeSizeBase - settings.HeadSize);

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

            // カメラ位置を更新
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
            float distance = _faceModeDistance;

            // 回転角度から顔の周囲の位置を計算
            float angleRad = _currentRotationAngle * Mathf.Deg2Rad;
            float x = targetPoint.x + distance * Mathf.Sin(angleRad);
            float z = targetPoint.z + distance * Mathf.Cos(angleRad);

            // 高さは顔の位置からオフセット
            float y = targetPoint.y + faceModeHeightOffset;

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

            // 新しい位置を設定（Y座標は初期位置と同じ）
            Vector3 newPosition = targetPoint + directionXZ * _initialDistance;
            newPosition.y = _initialPosition.y;

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
