using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    /// <summary>
    /// アバターパラメータリポジトリのインターフェース
    /// </summary>
    public interface IAvatarParameterRepository
    {
        UniTask<AvatarCustomizationSettings> LoadAsync(CancellationToken cancellationToken);
        UniTask SaveAsync(AvatarCustomizationSettings settings, CancellationToken cancellationToken);
    }
}