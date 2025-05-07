using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Domain.Interfaces
{
    /// <summary>
    /// 排他制御のインターフェース
    /// </summary>
    public interface IAsyncLockService
    {
        UniTask<IDisposable> LockAsync(CancellationToken cancellationToken = default);
    }
}
