using Cysharp.Threading.Tasks;
using System.Threading;
using Domain.ValueObjects;

namespace Domain.Interfaces
{
    public interface IAvatarLoader
    {
        UniTask<DomainLoadResult> LoadAvatarAsync(string avatarId, CancellationToken ct = default);
        bool UnloadAvatar(object avatarInstance);
    }
}
