using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using NUnit.Framework;
using UnityEngine.TestTools;
using NSubstitute;

using Infrastructure.Services;

namespace UnitTests.Infrastructure
{
    public class FirebaseAuthManagerServiceTests
    {
        private FirebaseAuthManagerService _authManagerService;
        private FirebaseAuth _mockAuth;
        private FirebaseUser _mockUser;

        [SetUp]
        public void Setup()
        {
            _authManagerService = new FirebaseAuthManagerService();
            _mockAuth = Substitute.For<FirebaseAuth>();
            _mockUser = Substitute.For<FirebaseUser>();

            _mockUser.Email.Returns("test@example.com");
        }

        [UnityTest]
        public IEnumerator 依存関係が利用可能な場合に初期化されること()
            => UniTask.ToCoroutine(async () =>
            {
                // Arrange
                using var cts = new CancellationTokenSource();

                // Act & Assert
                try
                {
                    await _authManagerService.InitializeAsync(cts.Token);
                    Assert.Pass("Initialization completed successfully");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Should not throw exception: {ex.Message}");
                }
            });

        [UnityTest]
        public IEnumerator 有効な資格情報でユーザー登録されること()
            => UniTask.ToCoroutine(async () =>
            {
                // Arrange
                using var cts = new CancellationTokenSource();
                const string email = "test@example.com";
                const string password = "password123";

                var mockAuthResult = Substitute.For<AuthResult>();
                mockAuthResult.User.Returns(_mockUser);

                _mockAuth.CreateUserWithEmailAndPasswordAsync(email, password)
                    .Returns(Task.FromResult(mockAuthResult));

                // Act & Assert
                try
                {
                    await _authManagerService.RegisterUserAsync(email, password, cts.Token);
                    Assert.Pass("User registration completed successfully");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Should not throw exception: {ex.Message}");
                }
            });

        [UnityTest]
        public IEnumerator 初期化なしでユーザー登録しようとすると例外がスローされること()
            => UniTask.ToCoroutine(async () =>
            {
                // Arrange
                using var cts = new CancellationTokenSource();
                const string email = "test@example.com";
                const string password = "password123";

                // Act & Assert
                try
                {
                    await _authManagerService.RegisterUserAsync(email, password, cts.Token);
                    Assert.Fail("Should throw InvalidOperationException");
                }
                catch (InvalidOperationException)
                {
                    Assert.Pass("Expected InvalidOperationException was thrown");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Wrong exception type was thrown: {ex.GetType()}");
                }
            });

        [UnityTest]
        public IEnumerator 有効な資格情報でサインインできること()
            => UniTask.ToCoroutine(async () =>
            {
                // Arrange
                using var cts = new CancellationTokenSource();
                const string email = "test@example.com";
                const string password = "password123";

                var mockAuthResult = Substitute.For<AuthResult>();
                mockAuthResult.User.Returns(_mockUser);

                _mockAuth.SignInWithEmailAndPasswordAsync(email, password)
                    .Returns(Task.FromResult(mockAuthResult));

                // Act & Assert
                try
                {
                    await _authManagerService.SignInUserAsync(email, password, cts.Token);
                    Assert.Pass("Sign in completed successfully");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Should not throw exception: {ex.Message}");
                }
            });

        [UnityTest]
        public IEnumerator 無効な資格情報でサインインしようとすると例外がスローされること()
            => UniTask.ToCoroutine(async () =>
            {
                // Arrange
                using var cts = new CancellationTokenSource();
                const string email = "test@example.com";
                const string password = "wrongpassword";

                _mockAuth.SignInWithEmailAndPasswordAsync(email, password)
                    .Returns(Task.FromException<AuthResult>(
                        new Exception("The password is invalid or the user does not have a password.")
                    ));

                // Act & Assert
                try
                {
                    await _authManagerService.SignInUserAsync(email, password, cts.Token);
                    Assert.Fail("Should throw Exception");
                }
                catch (Exception ex)
                {
                    Assert.That(ex.Message, Contains.Substring("password is invalid"));
                    Assert.Pass("Expected Exception was thrown");
                }
            });

        [UnityTest]
        public IEnumerator サインインキャンセル時にOperationCanceledExceptionがスローされること()
            => UniTask.ToCoroutine(async () =>
            {
                // Arrange
                using var cts = new CancellationTokenSource();
                const string email = "test@example.com";
                const string password = "password123";

                _mockAuth.SignInWithEmailAndPasswordAsync(email, password)
                    .Returns(Task.Delay(1000)); // Simulate long operation

                // Act & Assert
                try
                {
                    cts.Cancel();
                    await _authManagerService.SignInUserAsync(email, password, cts.Token);
                    Assert.Fail("Should throw OperationCanceledException");
                }
                catch (OperationCanceledException)
                {
                    Assert.Pass("Expected OperationCanceledException was thrown");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Wrong exception type was thrown: {ex.GetType()}");
                }
            });
    }
}
