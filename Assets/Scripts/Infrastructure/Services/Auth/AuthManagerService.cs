using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;

using Domain.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// Firebase 認証マネージャ
    /// </summary>
    /// <remarks>
    /// Firebase 認証の初期化、ユーザー登録、サインインを行う。
    /// </remarks>
    public class FirebaseAuthManagerService : IAuthManagerService
    {
        public FirebaseAuth Auth { get; private set; }
        public FirebaseUser CurrentUser { get; private set; }
        public string LastOperation { get; private set; }
        public string LastOperationDetails { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FirebaseAuthManagerService() { }

        /// <summary>
        /// Firebase の依存関係をチェックし、初期化する
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                LastOperation = "CheckingDependencies";
                LastOperationDetails = "Checking Firebase dependencies";

                // Firebase SDKの初期化前の状態確認
                if (FirebaseApp.DefaultInstance != null)
                {
                    LastOperation = "AlreadyInitialized";
                    LastOperationDetails = "Firebase already initialized";
                    Auth = FirebaseAuth.DefaultInstance;
                    return;
                }

                var dependencyStatus = await FirebaseApp
                    .CheckAndFixDependenciesAsync()
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);

                if (dependencyStatus == DependencyStatus.Available)
                {
                    Auth = FirebaseAuth.DefaultInstance;

                    // 既存のサインイン状態を確認
                    if (Auth.CurrentUser != null)
                    {
                        CurrentUser = Auth.CurrentUser;
                        LastOperation = "UserAlreadySignedIn";
                        LastOperationDetails = $"User already signed in: {CurrentUser.Email}";
                    }
                }
                else
                {
                    throw new Exception($"Firebase依存関係エラー: {dependencyStatus}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LastOperation = "InitializationFailed";
                LastOperationDetails = ex.Message;
                throw new Exception($"Firebase初期化失敗: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// メールアドレスとパスワードでユーザ登録を行う
        /// </summary>
        /// <param name="email">メールアドレス</param>
        /// <param name="password">パスワード</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async UniTask RegisterUserAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                LastOperation = "RegisteringUser";
                LastOperationDetails = $"Attempting to register user: {email}";

                if (Auth == null)
                {
                    throw new InvalidOperationException("Firebase Auth が初期化されていません");
                }

                var authResult = await Auth
                    .CreateUserWithEmailAndPasswordAsync(email, password)
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);

                CurrentUser = authResult.User;
                LastOperation = "UserRegistered";
                LastOperationDetails = $"User registered successfully: {CurrentUser.Email}";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FirebaseException firebaseEx)
            {
                LastOperation = "RegistrationFailed";
                LastOperationDetails = $"Firebase registration error: {firebaseEx.Message} (ErrorCode: {firebaseEx.ErrorCode})";
                throw new Exception($"Firebase登録エラー: {firebaseEx.Message} (ErrorCode: {firebaseEx.ErrorCode})");
            }
            catch (Exception ex)
            {
                LastOperation = "RegistrationFailed";
                LastOperationDetails = ex.Message;
                throw new Exception($"ユーザー登録失敗: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// メールアドレスとパスワードでユーザのサインインを行う
        /// </summary>
        /// <param name="email">メールアドレス</param>
        /// <param name="password">パスワード</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async UniTask SignInUserAsync(string email, string password, CancellationToken cancellationToken)
        {
            try
            {
                LastOperation = "SigningInUser";
                LastOperationDetails = $"Attempting to sign in user: {email}";

                if (Auth == null)
                {
                    throw new InvalidOperationException("Firebase Auth が初期化されていません");
                }

                var authResult = await Auth
                    .SignInWithEmailAndPasswordAsync(email, password)
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);

                CurrentUser = authResult.User;
                LastOperation = "UserSignedIn";
                LastOperationDetails = $"User signed in successfully: {CurrentUser.Email}";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FirebaseException firebaseEx)
            {
                LastOperation = "SignInFailed";
                LastOperationDetails = $"Firebase sign in error: {firebaseEx.Message} (ErrorCode: {firebaseEx.ErrorCode})";
                throw new Exception($"Firebase sign in error: {firebaseEx.Message} (ErrorCode: {firebaseEx.ErrorCode})");
            }
            catch (Exception ex)
            {
                LastOperation = "SignInFailed";
                LastOperationDetails = ex.Message;
                throw new Exception($"Sign in failed: {ex.Message}");
            }
        }
    }
}
