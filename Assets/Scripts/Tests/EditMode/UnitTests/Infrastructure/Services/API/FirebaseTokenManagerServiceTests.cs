using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

using Infrastructure.Services;

namespace UnitTests.Infrastructure
{
    public class FirebaseTokenManagerServiceTests
    {
        private FirebaseTokenManagerService _tokenManager;

        [SetUp]
        public void Setup()
        {
            _tokenManager = new FirebaseTokenManagerService();
        }

        [UnityTest]
        public IEnumerator 認証されていない場合_NetworkExceptionServiceがスローされること()
            => UniTask.ToCoroutine(async () =>
        {
            // Arrange & Act
            NetworkExceptionService actualException = null;
            try
            {
                await _tokenManager.GetAccessTokenAsync(CancellationToken.None);
                Assert.Fail("Expected NetworkExceptionService was not thrown");
            }
            catch (NetworkExceptionService ex)
            {
                actualException = ex;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception thrown: {ex.GetType().Name} - {ex.Message}");
            }

            // Assert
            Assert.That(actualException, Is.Not.Null);
            Assert.That(actualException.Message, Does.Contain("No authenticated user"));
        });

        [UnityTest]
        public IEnumerator キャンセル時にOperationCanceledExceptionがスローされること()
            => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            OperationCanceledException actualException = null;
            try
            {
                await _tokenManager.GetAccessTokenAsync(cts.Token);
                Assert.Fail("Expected OperationCanceledException was not thrown");
            }
            catch (OperationCanceledException ex)
            {
                actualException = ex;
            }

            // Assert
            Assert.That(actualException, Is.Not.Null);
        });
    }
}
