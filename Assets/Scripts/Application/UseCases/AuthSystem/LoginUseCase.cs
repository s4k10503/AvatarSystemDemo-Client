using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;
using Domain.ValueObjects;
using VContainer;

namespace Application.UseCases
{
    public sealed class LoginUseCase : IDisposable
    {
        private readonly IAuthManagerService _authManagerService;
        private readonly ILogService _logService;
        private readonly ITokenManagerService _tokenManagerService;
        private CancellationTokenSource _cts;
        private bool _isAuthenticated = false;
        private bool _isInitialized = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="authManagerService">認証マネージャーサービス</param>
        /// <param name="logService">ログサービス</param>
        /// <param name="tokenManagerService">トークンマネージャーサービス</param>
        [Inject]
        public LoginUseCase(
            IAuthManagerService authManagerService,
            ILogService logService,
            ITokenManagerService tokenManagerService)
        {
            _authManagerService = authManagerService;
            _logService = logService;
            _tokenManagerService = tokenManagerService;
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// 認証システムを初期化します。
        /// </summary>
        /// <returns>初期化が成功した場合はtrue、それ以外の場合はfalse。</returns>
        public async UniTask<bool> InitializeAsync()
        {
            try
            {
                // 既に初期化済みの場合はスキップ
                if (_isInitialized)
                {
                    _logService.Debug("Auth system already initialized, skipping initialization");
                    return true;
                }
                await _authManagerService.InitializeAsync(_cts.Token);
                _logService.Info($"Auth initialization: {_authManagerService.LastOperation} - {_authManagerService.LastOperationDetails}");
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logService.Warning($"Auth initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            if (_cts != null)
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
                _cts.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// アクセストークンを取得します。
        /// </summary>
        /// <returns>アクセストークン。認証されていない場合はnull。</returns>
        public async UniTask<string> GetAccessTokenAsync()
        {
            try
            {
                bool authenticated = _isAuthenticated || (_authManagerService.Auth != null && _authManagerService.Auth.CurrentUser != null);
                if (!authenticated)
                {
                    _logService.Warning("Cannot get token: User not authenticated");
                    return null;
                }

                return await _tokenManagerService.GetAccessTokenAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                _logService.Warning($"Failed to get access token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 認証状態を確認します。
        /// </summary>
        /// <returns>認証されている場合はtrue、それ以外の場合はfalse。</returns>
        public bool IsAuthenticated()
        {
            // 内部フラグと実際のFirebaseAuth状態の両方を確認
            return _isAuthenticated || (_authManagerService.Auth != null && _authManagerService.Auth.CurrentUser != null);
        }

        /// <summary>
        /// 認証状態を設定します。
        /// </summary>
        /// <param name="status">認証状態。trueの場合は認証されていることを意味します。</param>
        public void SetAuthenticationStatus(bool status)
        {
            if (_isAuthenticated != status)
            {
                _isAuthenticated = status;
                _logService.Info($"Authentication status changed to: {status}");
            }
        }

        /// <summary>
        /// トークンの更新状態を確認します。
        /// </summary>
        /// <returns>トークンが更新中の場合はtrue、それ以外の場合はfalse。</returns>
        public bool IsTokenRefreshing()
        {
            return _tokenManagerService.IsTokenRefreshing;
        }

        /// <summary>
        /// 現在認証されているユーザーのIDを取得します。
        /// </summary>
        /// <returns>ユーザーID。認証されていない場合はnull。</returns>
        public string GetCurrentUserId()
        {
            if (IsAuthenticated() && _authManagerService.Auth?.CurrentUser != null)
            {
                return _authManagerService.Auth.CurrentUser.UserId;
            }
            return null;
        }

        /// <summary>
        /// 資格情報を使用してユーザーをサインインします。
        /// </summary>
        /// <param name="email">ユーザーの電子メールアドレス。</param>
        /// <param name="password">ユーザーのパスワード。</param>
        /// <returns>サインインが成功した場合はtrue、それ以外の場合はfalse。</returns>
        public async UniTask<bool> SignInWithCredentialsAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    _logService.Warning("Cannot sign in: Empty credentials");
                    return false;
                }

                _logService.Debug($"Attempting to sign in user with credentials: {email}");
                await _authManagerService.SignInUserAsync(email, password, _cts.Token);

                _logService.Info($"Auth sign in successful: {_authManagerService.CurrentUser?.Email}");
                _isAuthenticated = true;
                return true;
            }
            catch (OperationCanceledException)
            {
                _logService.Warning("Authentication was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _logService.Warning($"Authentication failed: {ex.Message}");
                _isAuthenticated = false;
                return false;
            }
        }

        /// <summary>
        /// 新規ユーザー登録用のメソッド
        /// </summary>
        /// <param name="email">ユーザーの電子メールアドレス。</param>
        /// <param name="password">ユーザーのパスワード。</param>
        public async UniTask RegisterUserAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    _logService.Warning("Cannot register: Empty credentials");
                    throw new ArgumentException("Email and password cannot be empty");
                }

                _logService.Debug($"Attempting to register new user with email: {email}");
                await _authManagerService.RegisterUserAsync(email, password, _cts.Token);
                _logService.Info($"User registration successful for: {email}");

                // 登録後はまだログイン状態ではない
                _isAuthenticated = false;
            }
            catch (Exception ex)
            {
                _logService.Error($"User registration failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ユーザーをログアウトさせます
        /// </summary>
        public async UniTask LogoutAsync()
        {
            try
            {
                _logService.Debug("Logging out user");

                if (_authManagerService.Auth != null && _authManagerService.Auth.CurrentUser != null)
                {
                    // Firebaseからサインアウト
                    _authManagerService.Auth.SignOut();
                    _logService.Info("User signed out from Firebase");
                }

                // 認証状態をリセット
                _isAuthenticated = false;

                // 認証状態のクリア後に再初期化を行う
                await _authManagerService.InitializeAsync(_cts.Token);
                _logService.Info("Auth re-initialized after logout");

                return;
            }
            catch (Exception ex)
            {
                _logService.Error($"Logout failed: {ex.Message}");
                throw;
            }
        }
    }
}
