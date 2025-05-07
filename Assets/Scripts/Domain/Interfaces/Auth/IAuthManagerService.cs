using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;

namespace Domain.Interfaces
{
    /// <summary>
    /// 認証マネージャーサービスのインターフェース
    /// </summary>
    public interface IAuthManagerService
    {
        FirebaseAuth Auth { get; }
        FirebaseUser CurrentUser { get; }
        string LastOperation { get; }
        string LastOperationDetails { get; }
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        UniTask RegisterUserAsync(string email, string password, CancellationToken cancellationToken = default);
        UniTask SignInUserAsync(string email, string password, CancellationToken cancellationToken = default);
    }
}
