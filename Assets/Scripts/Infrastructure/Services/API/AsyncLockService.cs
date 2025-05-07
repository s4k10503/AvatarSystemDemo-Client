using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// 排他制御クラス
    /// </summary>
    public sealed class AsyncLockService : IAsyncLockService
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);

        /// <summary>
        /// 排他制御
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>IDisposable</returns>
        public async UniTask<IDisposable> LockAsync(CancellationToken ct)
        {
            await semaphore.WaitAsync(ct);
            return new Releaser(semaphore);
        }

        /// <summary>
        /// 排他制御解除
        /// </summary>
        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="semaphore">セマフォ</param>
            public Releaser(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            /// <summary>
            /// 破棄
            /// </summary>
            public void Dispose()
            {
                semaphore.Release();
            }
        }
    }
}
