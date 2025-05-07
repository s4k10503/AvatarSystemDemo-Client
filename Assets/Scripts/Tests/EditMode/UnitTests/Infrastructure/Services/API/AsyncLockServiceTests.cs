using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

using Infrastructure.Services;

namespace UnitTests.Infrastructure
{
    public class AsyncLockServiceTests
    {
        private AsyncLockService _asyncLockService;

        [SetUp]
        public void Setup()
        {
            _asyncLockService = new AsyncLockService();
        }

        [UnityTest]
        public IEnumerator ロック取得と解放が正しく行われること() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act & Assert
            using (var lockHandle = await _asyncLockService.LockAsync(cts.Token))
            {
                Assert.That(lockHandle, Is.Not.Null);
            }

            // Verify we can acquire the lock again after release
            Assert.DoesNotThrowAsync(async () =>
            {
                using var secondLock = await _asyncLockService.LockAsync(cts.Token);
            });
        });

        [UnityTest]
        public IEnumerator 複数タスクによる同時アクセスが防止されること() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            int sharedCounter = 0;
            const int iterations = 1000;
            const int numTasks = 5;

            async UniTask IncrementCounter()
            {
                for (int i = 0; i < iterations; i++)
                {
                    using (await _asyncLockService.LockAsync(cts.Token))
                    {
                        int temp = sharedCounter;
                        sharedCounter = temp + 1;
                    }
                }
            }

            // Act
            var tasks = new UniTask[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                tasks[i] = IncrementCounter();
            }
            await UniTask.WhenAll(tasks);

            // Assert
            Assert.That(sharedCounter, Is.EqualTo(iterations * numTasks));
        });

        [UnityTest]
        public IEnumerator キャンセル時にTaskCanceledExceptionがスローされること() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Acquire the lock first
            using var firstLock = await _asyncLockService.LockAsync(CancellationToken.None);

            // Act & Assert
            cts.Cancel();
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await _asyncLockService.LockAsync(cts.Token);
            });
        });

        [Test]
        public void Dispose呼び出しでロックが解放されること()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            IDisposable lockHandle = null;

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
            {
                lockHandle = await _asyncLockService.LockAsync(cts.Token);
                lockHandle.Dispose();

                // Should be able to acquire the lock immediately after disposal
                using var newLock = await _asyncLockService.LockAsync(cts.Token);
            });
        }
    }
}