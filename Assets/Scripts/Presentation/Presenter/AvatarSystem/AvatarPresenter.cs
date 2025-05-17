using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using System;
using R3;

using Domain.ValueObjects;
using Application.UseCases;
using Application.DTO;
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
        private GameObject _avatarRoot;
        private Animator _avatarAnimator;

        [Header("Avatar Settings")]
        [SerializeField] private string defaultAvatarPrefabName = "unitychan_dynamic Variant";

        private readonly AvatarCustomizationUseCase _customizationUseCase;
        private readonly LoginUseCase _loginUseCase;
        private readonly AvatarLifecycleUseCase _avatarLifecycleUseCase;

        private bool _isInitializing = false;
        private bool _avatarLoaded = false;

        private readonly CompositeDisposable _disposables = new();
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        [Inject]
        public AvatarPresenter(
            CameraView cameraView,
            AvatarSystemPage avatarSystemPage,
            AvatarLifecycleUseCase avatarLifecycleUseCase,
            AvatarCustomizationUseCase customizationUseCase,
            LoginUseCase loginUseCase,
            IPageManager pageManager
            )
        {
            _cameraView = cameraView;
            _avatarSystemPage = avatarSystemPage;
            _avatarLifecycleUseCase = avatarLifecycleUseCase;
            _customizationUseCase = customizationUseCase;
            _loginUseCase = loginUseCase;
            _pageManager = pageManager;


            // DIの検証
            if (_cameraView == null)
            {
                Debug.LogError("CameraView が注入されていません!");
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
                bool avatarSetupSuccess = await TryLoadAndSetupAvatarAsync();

                if (!avatarSetupSuccess)
                {
                    // アバターのセットアップに失敗した場合、必要に応じて追加のUIフィードバック
                    return;
                }

                // アバターロード成功後の処理
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

                // UIイベントの購読
                SubscribeToUIEvents();

                // 保存ボタンの状態を更新
                UpdateSaveButtonState();

                // アバターに変更を適用
                _customizationUseCase.ApplyCustomization();

                // アバターを表示
                if (_avatarRoot != null && _avatarAnimator != null)
                {
                    _avatarRoot.SetActive(true);
                    _cameraView.SetTarget(_avatarAnimator.transform);
                }

                // カメラビューに最新の設定を渡す（顔モード切替前に必ず最新の設定を反映）
                _cameraView.UpdateAvatarSettings(initialSettings);

                // カメラ回転イベントの購読
                if (EventSystem.current == null)
                {
                    var eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<EventSystem>();
                    eventSystem.AddComponent<StandaloneInputModule>();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("Initialization was cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"アバターカスタマイズの初期化に失敗しました: {ex.Message}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        /// アバターのロードとセットアップを試行する。
        /// 成功した場合は true を、失敗した場合は false を返す。
        /// </summary>
        private async UniTask<bool> TryLoadAndSetupAvatarAsync()
        {
            AvatarLoadResultDto loadResultDto;
            try
            {
                loadResultDto = await _avatarLifecycleUseCase.LoadAndSetupAvatarAsync(defaultAvatarPrefabName);
            }
            catch (OperationCanceledException) // AvatarLifecycleUseCaseから再スローされる
            {
                // UseCase側でInfoログが出力済み。ここではfalseを返すのみ。
                return false;
            }
            catch (Exception) // AvatarLifecycleUseCaseから再スローされる
            {
                // UseCase側でErrorログが出力済み。
                return false;
            }

            if (!loadResultDto.IsSuccess)
            {
                // UseCase側でErrorログが出力済み (loadResultDto.ErrorMessage を含む)
                return false;
            }

            if (loadResultDto.AvatarInstance is not GameObject go)
            {
                // UseCase側でErrorログが出力済みか、loadResultDto.ErrorMessage に詳細があるはず。
                return false;
            }

            _avatarRoot = go;
            // DTOからAnimatorの有無を取得
            if (loadResultDto.HasAnimator)
            {
                _avatarAnimator = _avatarRoot.GetComponent<Animator>();
                if (_avatarAnimator == null)
                {
                    // DTOではAnimatorありと報告されたが、GetComponentで取れなかった場合
                    _avatarLifecycleUseCase.ReleaseAvatar(_avatarRoot);
                    _avatarRoot = null;
                    return false;
                }
            }
            else
            {
                _avatarAnimator = null;
                // Animatorがない場合、それが許容されるかどうかのロジックがここに入る
                // 例えばAnimator必須ならエラーとしてfalseを返す
                // ShowAvatarLoadErrorFeedback("Avatar loaded but does not have an Animator component as per DTO.");
                // return false; // Animatorが必須の場合
            }

            _avatarLoaded = true;
            _avatarRoot.SetActive(false); // 初期は非表示。表示はInitializeAsyncの後半で行う。

            // Animator を AvatarCustomizationUseCase に設定
            if (_avatarAnimator != null && _customizationUseCase != null)
            {
                // _customizationUseCase に Animator を設定するメソッドを呼び出す
                // 例: _customizationUseCase.InitializeAnimator(_avatarAnimator);
                // このメソッドは AvatarCustomizationUseCase に実装する必要があります。
                // さらに AvatarCustomizationUseCase から AvatarCustomizationService へ、
                // そして AvatarColorServiceBase を継承するクラスへと Animator が伝播される想定です。
                // ここでは仮のメソッド名として InitializeAnimator を使用します。
                // 実際のメソッド名に合わせてください。
                _customizationUseCase.InitializeAnimator(_avatarAnimator);
            }

            return true; // セットアップ成功
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

                // カメラ回転イベントの購読
                _avatarSystemPage.OnCameraRotateRequested
                    .Subscribe(delta => { if (_cameraView != null) _cameraView.UpdateRotationByDelta(delta); })
                    .AddTo(_disposables);

                // カメラズームイベントの購読
                _avatarSystemPage.OnCameraZoomRequested
                    .Subscribe(pinchDelta =>
                    {
                        if (_cameraView != null) _cameraView.AdjustZoom(pinchDelta);
                    })
                    .AddTo(_disposables);

                // カメラ高さ調整イベントの購読
                _avatarSystemPage.OnCameraHeightRequested
                    .Subscribe(deltaY =>
                    {
                        if (_cameraView != null) _cameraView.AdjustHeight(deltaY);
                    })
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

            // アバターリソースの解放
            if (_avatarLoaded && _avatarRoot != null && _avatarLifecycleUseCase != null)
            {
                _avatarLifecycleUseCase.ReleaseAvatar(_avatarRoot);
            }
            _avatarRoot = null;
            _avatarAnimator = null;
            _avatarLoaded = false;
        }
    }
}
