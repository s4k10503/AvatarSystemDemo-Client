using UnityEngine;
using VContainer;
using Presentation.Interfaces;

namespace Presentation.View
{
    /// <summary>
    /// アバターの移動制御
    /// </summary>
    public sealed class AvatarMovementView : MonoBehaviour, IAvatarMovementController
    {
        #region Inspector Fields

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 2.0f;
        [SerializeField] private float _runSpeed = 5.0f;
        [SerializeField] private float _rotationSpeed = 10.0f;
        [SerializeField] private float _runThreshold = 0.7f;
        [SerializeField] private float _minMoveThreshold = 0.1f;

        [Header("Jump Settings")]
        [SerializeField] private float _jumpForce = 5.0f;
        [SerializeField] private float _gravity = 20.0f;

        [Header("Required Components")]
        [SerializeField] private Animator _animator;

        #endregion

        private IAvatarAnimationController _animationController;

        #region Private Fields

        // 移動状態
        private Vector3 _moveDirection = Vector3.zero;
        private float _currentSpeed = 0f;
        private bool _isRunning = false;

        // ジャンプ状態
        private float _verticalVelocity = 0f;
        private bool _isJumping = false;
        private bool _isGrounded = true;

        // キャッシュ
        private Transform _transform;
        private CharacterController _characterController;
        private static readonly Vector3 _upVector = Vector3.up;
        private static readonly Vector3 _zeroVector = Vector3.zero;

        #endregion

        /// <summary>
        /// アニメーションコントローラーを設定
        /// </summary>
        /// <param name="controller">アニメーションコントローラー</param>
        public void SetAnimationController(IAvatarAnimationController controller)
        {
            _animationController = controller;
        }

        #region Unity Lifecycle

        /// <summary>
        /// 初期化
        /// </summary>
        private void Awake()
        {
            _transform = transform;
            ValidateComponents();
        }

        /// <summary>
        /// 更新
        /// </summary>
        private void Update()
        {
            UpdateMovement();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 移動方向を設定
        /// </summary>
        public void SetMoveDirection(Vector2 direction)
        {
            if (!_isJumping)
            {
                UpdateMovementState(direction);
                UpdateRotation(direction);
                _animationController?.UpdateAnimation(GetMovementState());
            }
        }

        /// <summary>
        /// ジャンプを設定
        /// </summary>
        public void SetJump()
        {
            if (_isGrounded && !_isJumping)
            {
                _verticalVelocity = _jumpForce;
                _isJumping = true;
                _animationController?.UpdateAnimation(GetMovementState());
                Debug.Log("[AvatarMovementView] Jump!");
            }
        }

        /// <summary>
        /// 移動を停止
        /// </summary>
        public void StopMovement()
        {
            _moveDirection = _zeroVector;
            _currentSpeed = 0f;
            _isRunning = false;
            _animationController?.UpdateAnimation(GetMovementState());
        }

        /// <summary>
        /// 現在の移動状態を取得
        /// </summary>
        public AvatarMovementState GetMovementState()
        {
            return new AvatarMovementState(
                _moveDirection,
                _currentSpeed,
                _isRunning,
                _isJumping,
                _isGrounded
            );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// コンポーネントを検証
        /// </summary>
        private void ValidateComponents()
        {
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null)
            {
                Debug.LogError("[AvatarMovementView] CharacterController component is missing!");
            }

            if (_animator == null) // Validate the Animator assigned in Inspector
            {
                _animator = GetComponent<Animator>(); // Fallback if not assigned
                if (_animator == null)
                {
                    Debug.LogError("[AvatarMovementView] Animator component is missing or not assigned!");
                }
            }
        }

        /// <summary>
        /// 移動を更新
        /// </summary>
        private void UpdateMovement()
        {
            CheckGrounded();
            ApplyVerticalMovement();
            ApplyHorizontalMovement();
        }

        /// <summary>
        /// 移動状態を更新
        /// </summary>
        private void UpdateMovementState(Vector2 direction)
        {
            _moveDirection = new Vector3(direction.x, 0f, direction.y);
            _isRunning = direction.magnitude > _runThreshold;
            _currentSpeed = direction.magnitude * (_isRunning ? _runSpeed : _walkSpeed);
        }

        /// <summary>
        /// 回転を更新
        /// </summary>
        private void UpdateRotation(Vector2 direction)
        {
            if (direction.magnitude > _minMoveThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 接地をチェック
        /// </summary>
        private void CheckGrounded()
        {
            _isGrounded = _characterController.isGrounded;

            bool wasJumping = _isJumping;

            if (_isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = -2f;
                _isJumping = false;
            }

            if (wasJumping && !_isJumping)
            {
                _animationController?.UpdateAnimation(GetMovementState());
            }
        }

        /// <summary>
        /// 垂直方向の移動を適用
        /// </summary>
        private void ApplyVerticalMovement()
        {
            if (!_isGrounded)
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }

            Vector3 verticalMovement = _verticalVelocity * Time.deltaTime * _upVector;
            _characterController.Move(verticalMovement);
        }

        /// <summary>
        /// 水平方向の移動を適用
        /// </summary>
        private void ApplyHorizontalMovement()
        {
            if (_moveDirection.magnitude > _minMoveThreshold)
            {
                Vector3 horizontalMovement = _currentSpeed * Time.deltaTime * _moveDirection;
                _characterController.Move(horizontalMovement);
            }
        }

        #endregion
    }
}
