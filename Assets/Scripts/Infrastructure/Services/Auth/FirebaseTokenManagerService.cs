using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;

using Domain.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// Firebase トークンマネージャー
    /// </summary>
    /// <remarks>
    /// Firebase トークンの取得と更新を行う。
    /// </remarks>
    public class FirebaseTokenManagerService : ITokenManagerService
    {
        private string idToken;
        private DateTime expirationTime;

        private readonly FirebaseAuth firebaseAuth;
        private readonly AsyncLockService asyncLock = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FirebaseTokenManagerService()
        {
            // FirebaseAuth.DefaultInstance は FirebaseAuthManager.InitializeAsync() によって初期化済みであることが前提
            firebaseAuth = FirebaseAuth.DefaultInstance;
        }

        /// <summary>
        /// トークンを取得する
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>トークン</returns>
        public async UniTask<string> GetAccessTokenAsync(CancellationToken ct)
        {
            // IDトークンが期限切れの場合のみ更新
            if (string.IsNullOrEmpty(idToken) || DateTime.UtcNow >= expirationTime)
            {
                await RefreshTokenIfNeededAsync(ct);
            }
            return idToken;
        }

        /// <summary>
        /// トークンが更新中かどうかを示すフラグ
        /// </summary>
        public bool IsTokenRefreshing { get; private set; }

        /// <summary>
        /// トークンが必要な場合にトークンを更新する
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        private async UniTask RefreshTokenIfNeededAsync(CancellationToken ct)
        {
            using (await asyncLock.LockAsync(ct))
            {
                // 他のリクエストでトークンが更新済みか確認
                if (!string.IsNullOrEmpty(idToken) && DateTime.UtcNow < expirationTime)
                {
                    return;
                }

                IsTokenRefreshing = true;

                FirebaseUser user = firebaseAuth.CurrentUser ?? throw new NetworkExceptionService("No authenticated user. Please sign in first.");
                try
                {
                    // Task<string>からUniTask<string>への変換
                    idToken = await user.TokenAsync(true).AsUniTask().AttachExternalCancellation(ct);

                    // Firebase の ID トークンは通常 60 分有効。少し早めに更新するため 55 分とする
                    expirationTime = DateTime.UtcNow.AddMinutes(55);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new NetworkExceptionService($"Failed to refresh Firebase ID Token: {ex.Message}");
                }
                finally
                {
                    IsTokenRefreshing = false;
                }
            }
        }
    }
}
