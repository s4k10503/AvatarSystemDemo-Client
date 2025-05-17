using VContainer;
using VContainer.Unity;
using R3;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

using Application.UseCases;
using Presentation.View;
using Presentation.Interfaces;
using Presentation.Factories;
using Presentation.Events;

namespace Presentation.Presenter
{
    /// <summary>
    /// ルームシステム画面のPresenter
    /// </summary>
    public sealed class RoomSystemPagePresenter : IStartable, IDisposable
    {
        #region フィールド

        [Header("Input Settings")]
        [SerializeField] private float _slidePadActiveThreshold = 0.1f;  // スライドパッドがアクティブと判定する閾値
        [SerializeField] private int _debugLogPrecision = 2;            // デバッグログの数値精度

        private readonly RoomSystemPage _roomSystemPage;
        private IAvatarMovementController _loadedAvatarMovementController;
        private readonly IAvatarAnimationControllerFactory _animationControllerFactory;
        private readonly Subject<AvatarReadyEvent> _avatarReadyEventSubject;
        private readonly LoginUseCase _loginUseCase;
        private readonly IPageManager _pageManager;
        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private bool _avatarReady = false;
        private bool _isInitializing = false;

        // スライドパッドの入力状態
        private Vector2 _currentSlidePadDirection = Vector2.zero;
        private bool _isSlidePadActive = false;

        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="roomSystemPage">ルームシステムページ</param>
        /// <param name="avatarMovementView">アバター移動ビュー</param>
        /// <param name="animationControllerFactory">アバターアニメーションコントローラーファクトリー</param>
        /// <param name="avatarReadyEventSubject">アバター準備完了イベントサブジェクト</param>
        /// <param name="loginUseCase">ログインユースケース</param>
        /// <param name="pageManager">ページマネージャー</param>
        [Inject]
        public RoomSystemPagePresenter(
            RoomSystemPage roomSystemPage,
            IAvatarAnimationControllerFactory animationControllerFactory,
            Subject<AvatarReadyEvent> avatarReadyEventSubject,
            LoginUseCase loginUseCase,
            IPageManager pageManager)
        {
            _roomSystemPage = roomSystemPage;
            _animationControllerFactory = animationControllerFactory;
            _avatarReadyEventSubject = avatarReadyEventSubject;
            _loginUseCase = loginUseCase;
            _pageManager = pageManager;

            // DIの検証
            if (_roomSystemPage == null)
            {
                Debug.LogError("RoomSystemPage が注入されていません!");
            }
            if (_animationControllerFactory == null)
            {
                Debug.LogError("IAvatarAnimationControllerFactory が注入されていません!");
            }
            if (_avatarReadyEventSubject == null)
            {
                Debug.LogError("Subject<AvatarReadyEvent> が注入されていません!");
            }
            if (_loginUseCase == null)
            {
                Debug.LogError("LoginUseCase が注入されていません!");
            }
            if (_pageManager == null)
            {
                Debug.LogError("IPageManager が注入されていません!");
            }
        }

        /// <summary>
        /// 開始時の処理
        /// </summary>
        public void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            _isInitializing = true;
            try
            {
                // RoomSystemPageの初期化を待つ
                if (!_roomSystemPage.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("ルームシステムページがアクティブではありません。アクティブになったら購読を設定します。");
                }

                // アバター準備完了イベントを購読
                _avatarReadyEventSubject
                    .Subscribe(HandleAvatarReady)
                    .AddTo(_disposables);

                // アバターが準備できるまで待つ
                await UniTask.WaitUntil(() => _avatarReady, cancellationToken: _cancellationTokenSource.Token);

                // キャンセルされた場合の冗長なチェック、ただし安全なケース
                // もしWaitUntilが例外を投げない場合、_avatarReadyはtrueになるはずです。
                if (!_avatarReady)
                {
                    Debug.LogWarning("アバターが準備できませんでした（または準備前に初期化がキャンセルされました）。RoomSystemPagePresenterのUI購読はスキップされます。");
                    return;
                }

                // スライドパッドの方向変更を購読
                if (_roomSystemPage.SlidePadDirection != null)
                {
                    _roomSystemPage.SlidePadDirection
                        .Subscribe(direction => HandleSlidePadDirectionChanged(direction))
                        .AddTo(_disposables);
                }
                else
                {
                    Debug.LogError("RoomSystemPage.SlidePadDirection が null です!");
                }

                // ジャンプボタンのクリックを購読
                if (_roomSystemPage.OnJumpButtonClicked != null)
                {
                    _roomSystemPage.OnJumpButtonClicked
                        .Subscribe(_ => HandleJumpInput())
                        .AddTo(_disposables);
                }
                else
                {
                    Debug.LogError("RoomSystemPage.OnJumpButtonClicked が null です!");
                }

                // Logout button click
                if (_roomSystemPage.OnLogoutRequested != null)
                {
                    _roomSystemPage.OnLogoutRequested
                        .Subscribe(_ => HandleLogoutAsync().Forget())
                        .AddTo(_disposables);
                }
                else
                {
                    Debug.LogError("RoomSystemPage.OnLogoutRequested が null です!");
                }

                // Navigate to Avatar System button click
                if (_roomSystemPage.OnNavigateToAvatarSystemRequested != null)
                {
                    _roomSystemPage.OnNavigateToAvatarSystemRequested
                        .Subscribe(_ => _pageManager.NavigateTo(PageType.AvatarSystem))
                        .AddTo(_disposables);
                }
                else
                {
                    Debug.LogError("RoomSystemPage.OnNavigateToAvatarSystemRequested が null です!");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[RoomSystemPagePresenter] 初期化がキャンセルされました。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomSystemPagePresenter] 初期化に失敗しました: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        /// アバター準備完了イベントを処理
        /// </summary>
        /// <param name="e">アバター準備完了イベント</param>
        private void HandleAvatarReady(AvatarReadyEvent e)
        {
            if (e.AvatarAnimator != null && _animationControllerFactory != null)
            {
                _loadedAvatarMovementController = e.AvatarAnimator.GetComponent<IAvatarMovementController>();
                if (_loadedAvatarMovementController != null)
                {
                    var animationController = _animationControllerFactory.Create(e.AvatarAnimator);
                    _loadedAvatarMovementController.SetAnimationController(animationController);
                    _avatarReady = true;
                    Debug.Log("[RoomSystemPagePresenter] アバター準備完了イベントを受信し、IAvatarMovementController を取得、AnimationController を設定しました。");
                }
                else
                {
                    Debug.LogError("[RoomSystemPagePresenter] AvatarReadyEvent を受信しましたが、ロードされたアバターに IAvatarMovementController が見つかりませんでした。");
                }
            }
            else
            {
                Debug.LogError("[RoomSystemPagePresenter] アバター準備完了イベントを受信しましたが、AnimationControllerの設定に必要な情報が不足しています。");
                if (e.AvatarAnimator == null) Debug.LogError("アバターAnimatorがnullです。");
                if (_animationControllerFactory == null) Debug.LogError("アバターアニメーションコントローラーファクトリーがnullです。");
            }
        }

        /// <summary>
        /// スライドパッドの方向変更を処理
        /// </summary>
        /// <param name="direction">スライドパッドの方向ベクトル</param>
        private void HandleSlidePadDirectionChanged(Vector2 direction)
        {
            // 方向ベクトルの大きさをチェックして、スライドパッドがアクティブかどうかを判定
            _isSlidePadActive = direction.magnitude > _slidePadActiveThreshold;
            _currentSlidePadDirection = direction;

            if (_isSlidePadActive)
            {
                // アバターの移動方向を設定
                // Check if avatar is ready and view is available, although _avatarReady should be true if subscriptions are set up.
                if (_avatarReady && _loadedAvatarMovementController != null)
                {
                    _loadedAvatarMovementController.SetMoveDirection(direction);
                }
                else if (!_avatarReady)
                {
                    Debug.LogWarning("[RoomSystemPagePresenter] スライドパッドの方向変更を処理しましたが、アバターが準備できていません。");
                }


                // デバッグ情報の出力
                LogSlidePadInput(direction);
            }
            else
            {
                // スライドパッドが非アクティブな場合は移動を停止
                _loadedAvatarMovementController?.StopMovement();
                Debug.Log("スライドパッド: ニュートラル");
            }
        }

        /// <summary>
        /// スライドパッドの入力をログ出力
        /// </summary>
        /// <param name="direction">スライドパッドの方向ベクトル</param>
        private void LogSlidePadInput(Vector2 direction)
        {
            Vector2 normalizedDirection = direction.normalized;
            float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;

            string format = $"F{_debugLogPrecision}";
            Debug.Log($"スライドパッド入力: " +
                $"方向=({normalizedDirection.x.ToString(format)}, {normalizedDirection.y.ToString(format)}), " +
                $"角度={angle.ToString(format)}°, " +
                $"強度={direction.magnitude.ToString(format)}");
        }

        /// <summary>
        /// ジャンプ入力を処理
        /// </summary>
        private void HandleJumpInput()
        {
            if (_avatarReady && _loadedAvatarMovementController != null) // Check if avatar is ready
            {
                _loadedAvatarMovementController.SetJump();
                Debug.Log("ジャンプ入力");
            }
            else
            {
                Debug.LogWarning("[RoomSystemPagePresenter] アバターが準備できていないか、移動ビューがnullのためジャンプ入力を処理できません。");
            }
        }

        /// <summary>
        /// ログメッセージを処理
        /// </summary>
        /// <param name="message">ログメッセージ</param>
        private void HandleLogMessage(string message)
        {
            // デバッグ用のログメッセージを処理
            Debug.Log(message);
        }

        /// <summary>
        /// ログアウト要求を処理
        /// </summary>
        private async UniTask HandleLogoutAsync()
        {
            Debug.Log("[RoomSystemPagePresenter] ログアウト要求を受信しました。");
            try
            {
                await _loginUseCase.LogoutAsync();
                _pageManager.NavigateTo(PageType.Login);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomSystemPagePresenter] ログアウトに失敗しました: {ex.Message}");
                // Optionally, show an error message to the user on the UI
            }
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        public void Dispose()
        {
            _disposables?.Clear();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}
