using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.Interfaces
{
    /// <summary>
    /// トークンマネージャーサービスのインターフェース
    /// </summary>
    public interface ITokenManagerService
    {
        UniTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
        bool IsTokenRefreshing { get; }
    }
}
