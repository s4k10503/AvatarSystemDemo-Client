using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using System;
using R3;

using Domain.ValueObjects;
using Application.UseCases;
using Presentation.View;
using Presentation.Interfaces;


namespace Presentation.Presenter
{
    /// <summary>
    /// アバターの表示を管理するPresenter
    /// </summary>
    /// <remarks>
    /// アバターの表示を管理するPresenter。
    /// </remarks>
    public sealed class AvatarPresenter : IStartable, IDisposable
    {
        #region フィールド
        private readonly IPageManager _pageManager;
        private readonly CameraView _cameraView;
        private readonly AvatarSystemPage _avatarSystemPage;
        private readonly GameObject _avatarRoot;
        private readonly Animator _avatarAnimator;

        private readonly AvatarCustomizationUseCase _customizationUseCase;
        private readonly LoginUseCase _loginUseCase;

        private bool _isInitializing = false;

        private readonly CompositeDisposable _disposables = new();
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        [Inject]
        public AvatarPresenter(
            CameraView cameraView,
            AvatarSystemPage avatarSystemPage,
            GameObject avatarRoot,
            AvatarCustomizationUseCase customizationUseCase,
            LoginUseCase loginUseCase,
            IPageManager pageManager)
        {
            _cameraView = cameraView;
            _avatarSystemPage = avatarSystemPage;
            _avatarRoot = avatarRoot;
            _customizationUseCase = customizationUseCase;
            _loginUseCase = loginUseCase;
            _pageManager = pageManager;
            _avatarAnimator = avatarRoot.GetComponent<Animator>();

            // 初期状態で非表示
            _avatarRoot.SetActive(false);

            // DIの検証
            if (_cameraView == null)
            {
                Debug.LogError("CameraView が注入されていません!");
            }

            if (_avatarRoot == null)
            {
                Debug.LogError("AvatarRoot が注入されていません!");
            }

            if (_avatarSystemPage == null)
            {
                Debug.LogError("AvatarSystemPage が注入されていません!");
            }
        }

        /// <summary>
        /// 開始時の処理
        /// </summary>
        public void Start()
        {
            InitializeAsync().Forget();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private async UniTask InitializeAsync()
        {
            _isInitializing = true;
            try
            {
                // 認証状態とトークンを取得
                bool isAuthenticated = _loginUseCase.IsAuthenticated();
                string token = null;

                if (isAuthenticated)
                {
                    try
                    {
                        token = await _loginUseCase.GetAccessTokenAsync();
                        if (string.IsNullOrEmpty(token))
                        {
                            NavigateToLoginPage();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"トークンエラー: {ex.Message}");
                        NavigateToLoginPage();
                        return;
                    }
                }

                // ユーザーIDを取得
                string userId = null;
                if (isAuthenticated)
                {
                    userId = _loginUseCase.GetCurrentUserId();
                    if (string.IsNullOrEmpty(userId))
                    {
                        Debug.LogWarning("ユーザーは認証されていますが、LoginUseCaseからUserIdを取得できませんでした。");
                        // 必要に応じてここでログインページに戻るなどのエラー処理を追加
                    }
                }

                // クラウドまたはローカルからデータをロード (userId を渡す)
                await _customizationUseCase.InitializeAsync(isAuthenticated, token, userId);
                var initialSettings = _customizationUseCase.GetCurrentSettings();

                // UIを更新
                if (_avatarSystemPage != null)
                {
                    _avatarSystemPage.UpdateUIValues(
                        initialSettings.Height,
                        initialSettings.ShoulderWidth,
                        initialSettings.BodyWidth,
                        initialSettings.HeadSize,
                        initialSettings.SkinColor,
                        initialSettings.HairColor
                    );
                }

                // UIイベントの購読を開始
                SubscribeToUIEvents();

                // ボタン状態を更新 (UI更新後、イベント購読後)
                UpdateSaveButtonState();

                // アバターに変更を適用 (UI更新後)
                _customizationUseCase.ApplyCustomization();

                // アバターを表示
                _avatarRoot.SetActive(true);
                _cameraView.SetTarget(_avatarAnimator.transform);

                // カメラビューにアバター設定を伝える
                _cameraView.UpdateAvatarSettings(initialSettings);

                if (EventSystem.current == null)
                {
                    var eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<EventSystem>();
                    eventSystem.AddComponent<StandaloneInputModule>();
                }

                // ボタン状態を更新 (UI更新後、イベント購読後)
                UpdateSaveButtonState();

                // アバターに変更を適用 (UI更新後)
                _customizationUseCase.ApplyCustomization();

                // カメラビューにアバター設定を伝える
                _cameraView.UpdateAvatarSettings(initialSettings);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時の処理
            }
            catch (Exception ex)
            {
                Debug.LogError($"アバターカスタマイズの初期化に失敗しました: {ex.Message}");
                _avatarRoot.SetActive(true);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        /// ログインページに遷移する
        /// </summary>
        private void NavigateToLoginPage()
        {
            // ログインページに遷移
            _pageManager.NavigateTo(PageType.Login);
        }

        /// <summary>
        /// 身長が変更された時の処理
        /// </summary>
        private void HandleHeightChanged(float value)
        {
            if (_isInitializing) return;

            try
            {
                // 現在の設定を更新
                _customizationUseCase.UpdateSettings(
                    value,
                    _customizationUseCase.GetCurrentSettings().ShoulderWidth,
                    _customizationUseCase.GetCurrentSettings().BodyWidth,
                    _customizationUseCase.GetCurrentSettings().HeadSize
                );

                // アバターに変更を適用
                _customizationUseCase.ApplyCustomization();

                // ヘッド位置情報を更新
                UpdateHeadPositionForCamera();

                // 保存ボタンの状態を更新
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"身長の更新に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 肩幅が変更された時の処理
        /// </summary>
        private void HandleShoulderWidthChanged(float value)
        {
            if (_isInitializing) return;

            try
            {
                // 現在の設定を更新
                _customizationUseCase.UpdateSettings(
                    _customizationUseCase.GetCurrentSettings().Height,
                    value,
                    _customizationUseCase.GetCurrentSettings().BodyWidth,
                    _customizationUseCase.GetCurrentSettings().HeadSize
                );

                // アバターに変更を適用
                _customizationUseCase.ApplyCustomization();

                // ヘッド位置情報を更新
                UpdateHeadPositionForCamera();

                // 保存ボタンの状態を更新
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"肩幅の更新に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 体の横幅が変更された時の処理
        /// </summary>
        private void HandleBodyWidthChanged(float value)
        {
            if (_isInitializing) return;

            try
            {
                // 現在の設定を更新
                _customizationUseCase.UpdateSettings(
                    _customizationUseCase.GetCurrentSettings().Height,
                    _customizationUseCase.GetCurrentSettings().ShoulderWidth,
                    value,
                    _customizationUseCase.GetCurrentSettings().HeadSize
                );

                // アバターに変更を適用
                _customizationUseCase.ApplyCustomization();

                // ヘッド位置情報を更新
                UpdateHeadPositionForCamera();

                // 保存ボタンの状態を更新
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"体の横幅の更新に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 頭の大きさが変更された時の処理
        /// </summary>
        private void HandleHeadSizeChanged(float value)
        {
            if (_isInitializing) return;

            try
            {
                // 現在の設定を更新
                _customizationUseCase.UpdateSettings(
                    _customizationUseCase.GetCurrentSettings().Height,
                    _customizationUseCase.GetCurrentSettings().ShoulderWidth,
                    _customizationUseCase.GetCurrentSettings().BodyWidth,
                    value
                );

                // アバターに変更を適用
                _customizationUseCase.ApplyCustomization();

                // ヘッド位置情報を更新
                UpdateHeadPositionForCamera();

                // 保存ボタンの状態を更新
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"頭の大きさの更新に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// カメラビュー用のヘッド位置情報を更新する
        /// </summary>
        private void UpdateHeadPositionForCamera()
        {
            if (_cameraView == null || _avatarRoot == null) return;

            // ヘッドボーン位置の取得
            var headInfo = _customizationUseCase.GetHeadPosition(_avatarRoot);
            if (headInfo.HasValue)
            {
                var (headOffset, headPosition) = headInfo.Value;

                // カメラビューに頭部位置情報を渡す
                _cameraView.SetHeadPosition(headOffset, headPosition);
            }
            else
            {
                Debug.LogWarning("ヘッド位置情報を取得できませんでした");
            }
        }

        /// <summary>
        /// 保存ボタンの状態を更新する
        /// </summary>
        private void UpdateSaveButtonState()
        {
            if (_avatarSystemPage != null)
            {
                // ユースケースの状態をそのまま反映
                _avatarSystemPage.UpdateSaveButtonState(_customizationUseCase.HasUnsavedChanges());
            }
        }

        /// <summary>
        /// 保存ボタンが押された時の処理
        /// </summary>
        private async void HandleSaveRequested()
        {
            try
            {
                // トークンを取得してからクラウド保存処理を実行
                string token = null;
                if (_loginUseCase.IsAuthenticated())
                {
                    try
                    {
                        token = await _loginUseCase.GetAccessTokenAsync();
                        if (string.IsNullOrEmpty(token))
                        {
                            // トークンが期限切れの場合
                            NavigateToLoginPage();
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        // トークン取得エラーの場合はログインページに遷移
                        NavigateToLoginPage();
                        return;
                    }
                }

                await _customizationUseCase.SaveSettingsAsync(token);

                // 保存後、ユースケースの状態を反映
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"アバター設定の保存に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// リセットボタンが押された時の処理
        /// </summary>
        private async void HandleResetRequested()
        {
            try
            {
                // トークンを取得してからリセット処理を実行
                string token = null;
                if (_loginUseCase.IsAuthenticated())
                {
                    try
                    {
                        token = await _loginUseCase.GetAccessTokenAsync();
                        if (string.IsNullOrEmpty(token))
                        {
                            // トークンが期限切れの場合
                            NavigateToLoginPage();
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        // トークン取得エラーの場合はログインページに遷移
                        NavigateToLoginPage();
                        return;
                    }
                }

                await _customizationUseCase.ResetToDefaultAsync(token);

                // 現在の設定を取得してUIを更新
                var currentSettings = _customizationUseCase.GetCurrentSettings();
                _avatarSystemPage.UpdateUIValues(
                    currentSettings.Height,
                    currentSettings.ShoulderWidth,
                    currentSettings.BodyWidth,
                    currentSettings.HeadSize,
                    currentSettings.SkinColor,
                    currentSettings.HairColor
                );

                // リセット後、保存ボタンの状態を更新
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"アバター設定のリセットに失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// ログアウト処理メソッド
        /// </summary>
        private void HandleLogoutRequested()
        {
            Debug.Log("ユーザーがログアウトしました");

            // 保存していない変更がある場合は保存
            if (_customizationUseCase.HasUnsavedChanges())
            {
                try
                {
                    _customizationUseCase.SaveSettingsAsync().Forget();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ログアウト前の設定の保存に失敗しました: {ex.Message}");
                }
            }

            // ログアウト処理の実行
            try
            {
                _loginUseCase.LogoutAsync().Forget();
                // ログインページへ遷移
                NavigateToLoginPage();
            }
            catch (Exception ex)
            {
                Debug.LogError($"ログアウトに失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// UIイベントの購読を開始
        /// </summary>
        private void SubscribeToUIEvents()
        {
            if (_avatarSystemPage != null)
            {
                _avatarSystemPage.Height
                    .Subscribe(HandleHeightChanged)
                    .AddTo(_disposables);

                _avatarSystemPage.ShoulderWidth
                    .Subscribe(HandleShoulderWidthChanged)
                    .AddTo(_disposables);

                _avatarSystemPage.BodyWidth
                    .Subscribe(HandleBodyWidthChanged)
                    .AddTo(_disposables);

                _avatarSystemPage.HeadSize
                    .Subscribe(HandleHeadSizeChanged)
                    .AddTo(_disposables);

                _avatarSystemPage.SkinColor
                    .Subscribe(HandleSkinColorChanged)
                    .AddTo(_disposables);

                _avatarSystemPage.HairColor
                    .Subscribe(HandleHairColorChanged)
                    .AddTo(_disposables);

                _avatarSystemPage.OnSaveRequested
                    .Subscribe(_ => HandleSaveRequested())
                    .AddTo(_disposables);

                _avatarSystemPage.OnResetRequested
                    .Subscribe(_ => HandleResetRequested())
                    .AddTo(_disposables);

                _avatarSystemPage.OnLogoutRequested
                    .Subscribe(_ => HandleLogoutRequested())
                    .AddTo(_disposables);

                // UI表示状態変更イベントの購読
                _avatarSystemPage.OnUIVisibilityChanged
                    .Subscribe(isVisible =>
                    {
                    })
                    .AddTo(_disposables);

                // タブ切り替えイベントの購読
                _avatarSystemPage.OnTabChanged
                    .Subscribe(tabIndex => HandleTabChanged(tabIndex))
                    .AddTo(_disposables);

                // Added: Subscribe to camera rotation requests from the page
                _avatarSystemPage.OnCameraRotateRequested
                    .Subscribe(delta => _cameraView?.UpdateRotationByDelta(delta))
                    .AddTo(_disposables);
            }
        }

        /// <summary>
        /// タブ切り替え時の処理
        /// </summary>
        private void HandleTabChanged(int tabIndex)
        {
            if (_cameraView == null) return;

            // 現在のアバター設定を取得
            var currentSettings = _customizationUseCase.GetCurrentSettings();

            // カメラビューに最新の設定を渡す（顔モード切替前に必ず最新の設定を反映）
            _cameraView.UpdateAvatarSettings(currentSettings);

            // ヘッドボーン位置の取得（ユースケースから取得）
            var headInfo = _customizationUseCase.GetHeadPosition(_avatarRoot);
            if (headInfo.HasValue)
            {
                var (headOffset, headPosition) = headInfo.Value;

                // カメラビューに頭部位置情報を渡す
                _cameraView.SetHeadPosition(headOffset, headPosition);
            }
            else
            {
                Debug.LogWarning("ヘッド位置情報を取得できませんでした");
            }

            // tabIndex: 0 = 体, 1 = 顔, 2 = 髪
            // 顔タブまたは髪タブが選択された場合に顔モードにする
            bool isFaceMode = tabIndex == 1 || tabIndex == 2;

            // カメラビューの顔モードを切り替え
            _cameraView.SetFaceMode(isFaceMode);

            if (isFaceMode)
            {
            }
        }

        /// <summary>
        /// 肌色が変更された時の処理
        /// </summary>
        private void HandleSkinColorChanged(SkinColor skinColor)
        {
            if (_isInitializing) return;

            Debug.Log($"[AvatarPresenter] 肌色変更ハンドラー呼び出し: R:{skinColor.Value.R} G:{skinColor.Value.G} B:{skinColor.Value.B}");
            try
            {
                // 現在の設定を更新
                _customizationUseCase.UpdateSkinColor(skinColor);

                // アバターに変更を適用
                _customizationUseCase.ApplyCustomization();

                // 保存ボタンの状態を更新
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"肌色の更新に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 髪色が変更された時の処理
        /// </summary>
        private void HandleHairColorChanged(HairColor hairColor)
        {
            if (_isInitializing) return;

            Debug.Log($"[AvatarPresenter] 髪色変更ハンドラー呼び出し: R:{hairColor.Value.R} G:{hairColor.Value.G} B:{hairColor.Value.B}");
            try
            {
                // 現在の設定を更新
                _customizationUseCase.UpdateHairColor(hairColor);
                _customizationUseCase.ApplyCustomization();
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"髪色の更新に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        public void Dispose()
        {
            // 全てのイベント購読を解除
            _disposables.Dispose();
        }
    }
}
