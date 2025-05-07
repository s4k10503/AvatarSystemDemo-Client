using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using System;
using R3;
using UnityEngine;

using Application.UseCases;
using Presentation.View;
using Presentation.Interfaces;

namespace Presentation.Presenter
{
    /// <summary>
    /// ログイン画面のPresenter
    /// </summary>
    /// <remarks>
    /// ログイン画面のPresenter。
    /// </remarks>
    public sealed class LoginPresenter : IStartable, IDisposable
    {
        private readonly LoginPage _loginPage;
        private readonly LoginModal _loginModal;

        private readonly LoginUseCase _loginUseCase;
        private readonly IPageManager _pageManager;

        private readonly CompositeDisposable _disposables = new();

        // 定義済みのエラーメッセージ
        private const string ERROR_INVALID_EMAIL = "有効なメールアドレスを入力してください。";
        private const string ERROR_WRONG_PASSWORD = "パスワードが間違っています。";
        private const string ERROR_USER_NOT_FOUND = "アカウントが見つかりません。";
        private const string ERROR_EMAIL_IN_USE = "このメールアドレスは既に使用されています。";
        private const string ERROR_WEAK_PASSWORD = "パスワードが弱すぎます。";
        private const string ERROR_GENERAL = "エラーが発生しました。もう一度お試しください。";
        private const string ERROR_LOGIN_FAILED = "ログインに失敗しました。メールアドレスとパスワードを確認してください。";
        private const string SUCCESS_REGISTER = "登録が完了しました。ログインしてください。";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        [Inject]
        public LoginPresenter(
            LoginPage loginPage,
            LoginModal loginModal,
            LoginUseCase loginUseCase,
            IPageManager pageManager)
        {
            _loginPage = loginPage;
            _loginModal = loginModal;
            _loginUseCase = loginUseCase;
            _pageManager = pageManager;

            // DIの検証
            if (_loginPage == null)
            {
                Debug.LogError("LoginPage が注入されていません!");
            }

            if (_loginModal == null)
            {
                Debug.LogError("LoginModal が注入されていません!");
            }
        }

        /// <summary>
        /// 開始時の処理
        /// </summary>
        public void Start()
        {
            // ログインページのイベントを購読
            _loginPage.OnLoginRequested
                .Subscribe(_ => ShowLoginModal())
                .AddTo(_disposables);

            _loginPage.OnContinueAsGuestRequested
                .Subscribe(_ => HandleContinueAsGuestRequested())
                .AddTo(_disposables);

            // ログインモーダルのイベントを購読
            _loginModal.OnLoginRequested
                .Subscribe(credentials => HandleLoginWithCredentialsRequested(credentials.email, credentials.password))
                .AddTo(_disposables);

            _loginModal.OnRegisterRequested
                .Subscribe(credentials => HandleRegisterRequested(credentials.email, credentials.password))
                .AddTo(_disposables);

            _loginModal.OnCancelRequested
                .Subscribe(_ => HandleLoginCancelled())
                .AddTo(_disposables);

            // 認証状態チェック
            CheckAuthenticationStatus().Forget();
        }

        /// <summary>
        /// 認証状態チェック
        /// </summary>
        private async UniTask CheckAuthenticationStatus()
        {
            try
            {
                // 認証状態とトークンを取得
                bool isAuthenticated = _loginUseCase.IsAuthenticated();

                if (isAuthenticated)
                {
                    try
                    {
                        string token = await _loginUseCase.GetAccessTokenAsync();
                        if (string.IsNullOrEmpty(token))
                        {
                            // トークンが空の場合は期限切れと判断
                            ShowLoginModal();
                        }
                        else
                        {
                            // 認証済みかつトークンが有効な場合、アバターページに遷移
                            NavigateToAvatarPage();
                        }
                    }
                    catch (Exception ex)
                    {
                        // トークン取得に失敗した場合（期限切れなど）
                        Debug.LogWarning($"トークンエラー: {ex.Message}");
                        ShowLoginModal();
                    }
                }
                // ログインページにはすでにいるので、未認証の場合は何もしない
            }
            catch (Exception ex)
            {
                Debug.LogError($"認証状態チェックに失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// ログインモーダルを表示
        /// </summary>
        public void ShowLoginModal()
        {
            _loginPage.ShowLoginModal();
        }

        /// <summary>
        /// ゲストとして続ける場合はアバターページに遷移
        /// </summary>
        private void HandleContinueAsGuestRequested()
        {
            // ゲストとして続ける場合はアバターページに遷移
            NavigateToAvatarPage();
        }

        /// <summary>
        /// ログインキャンセル
        /// </summary>
        private void HandleLoginCancelled()
        {
            // モーダルを閉じるだけで、画面遷移はしない
        }

        /// <summary>
        /// ログインキャンセル
        /// </summary>
        private async void HandleLoginWithCredentialsRequested(string email, string password)
        {
            try
            {
                Debug.Log($"ログイン処理開始: {email}");

                // メールアドレスの基本的な検証
                if (!IsValidEmail(email))
                {
                    _loginModal.ShowLoginError(ERROR_INVALID_EMAIL);
                    return;
                }

                // Firebase Authの初期化は不要 (InitializationPresenterで行われる)

                bool success = await _loginUseCase.SignInWithCredentialsAsync(email, password);
                Debug.Log($"ログイン結果: {success}");

                if (success)
                {
                    _loginModal.Hide();
                    Debug.Log("ログイン成功: モーダルを閉じてトークン取得開始");

                    string token = await _loginUseCase.GetAccessTokenAsync();
                    Debug.Log($"トークン取得: {(string.IsNullOrEmpty(token) ? "失敗" : "成功")}");

                    // ログイン成功後、アバターページに遷移
                    NavigateToAvatarPage();
                }
                else
                {
                    Debug.LogWarning("ログイン失敗: エラーメッセージを表示");
                    _loginModal.ShowLoginError(ERROR_LOGIN_FAILED);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ログインエラー: {ex.Message}");
                string errorMessage = GetUserFriendlyErrorMessage(ex.Message);
                _loginModal.ShowLoginError(errorMessage);
            }
        }

        /// <summary>
        /// 新規登録
        /// </summary>
        private async void HandleRegisterRequested(string email, string password)
        {
            try
            {
                Debug.Log($"新規登録処理開始: {email}");

                // メールアドレスの基本的な検証
                if (!IsValidEmail(email))
                {
                    _loginModal.ShowRegisterError(ERROR_INVALID_EMAIL);
                    return;
                }

                // 新規ユーザー登録処理
                await _loginUseCase.InitializeAsync();
                await _loginUseCase.RegisterUserAsync(email, password);

                Debug.Log("ユーザー登録成功");
                _loginModal.ShowRegisterSuccess(SUCCESS_REGISTER);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ユーザー登録エラー: {ex.Message}");
                // Firebaseのエラーメッセージをユーザーフレンドリーなものに変換
                string errorMessage = GetUserFriendlyErrorMessage(ex.Message);
                _loginModal.ShowRegisterError(errorMessage);
            }
        }

        /// <summary>
        /// アバターページに遷移
        /// </summary>
        private void NavigateToAvatarPage()
        {
            // アバターカスタマイズページに遷移
            _pageManager.NavigateTo(PageType.AvatarSystem);
        }

        /// <summary>
        /// メールアドレスの基本的な検証
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;

            // 基本的な検証（@が含まれているか）
            return email.Contains("@") && email.Contains(".");
        }

        /// <summary>
        /// ユーザーフレンドリーなエラーメッセージに変換
        /// </summary>
        private string GetUserFriendlyErrorMessage(string originalMessage)
        {
            if (originalMessage.Contains("invalid-email"))
                return ERROR_INVALID_EMAIL;
            if (originalMessage.Contains("wrong-password"))
                return ERROR_WRONG_PASSWORD;
            if (originalMessage.Contains("user-not-found"))
                return ERROR_USER_NOT_FOUND;
            if (originalMessage.Contains("email-already-in-use"))
                return ERROR_EMAIL_IN_USE;
            if (originalMessage.Contains("weak-password"))
                return ERROR_WEAK_PASSWORD;

            return ERROR_GENERAL;
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        public void Dispose()
        {
            _disposables.Clear();
        }
    }
}
