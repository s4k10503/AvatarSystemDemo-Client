using NUnit.Framework;
using System;
using Infrastructure.Services;

namespace UnitTests.Infrastructure
{
    public class NetworkExceptionServiceTests
    {
        [Test]
        public void NetworkExceptionService_メッセージ付き_正しい例外が生成されること()
        {
            // Arrange
            const string errorMessage = "Network connection failed";

            // Act
            var exception = new NetworkExceptionService(errorMessage);

            // Assert
            Assert.That(exception, Is.InstanceOf<Exception>());
            Assert.That(exception.Message, Is.EqualTo(errorMessage));
        }

        [Test]
        public void VersionMismatchException_メッセージ付き_正しい例外が生成されること()
        {
            // Arrange
            const string errorMessage = "Version mismatch detected";

            // Act
            var exception = new VersionMismatchException(errorMessage);

            // Assert
            Assert.That(exception, Is.InstanceOf<Exception>());
            Assert.That(exception.Message, Is.EqualTo(errorMessage));
        }

        [Test]
        public void MasterDataMismatchException_メッセージ付き_正しい例外が生成されること()
        {
            // Arrange
            const string errorMessage = "Master data version mismatch";

            // Act
            var exception = new MasterDataMismatchException(errorMessage);

            // Assert
            Assert.That(exception, Is.InstanceOf<Exception>());
            Assert.That(exception.Message, Is.EqualTo(errorMessage));
        }

        [Test]
        public void DuplicateRequestException_メッセージ付き_正しい例外が生成されること()
        {
            // Arrange
            const string errorMessage = "Duplicate request detected";

            // Act
            var exception = new DuplicateRequestException(errorMessage);

            // Assert
            Assert.That(exception, Is.InstanceOf<Exception>());
            Assert.That(exception.Message, Is.EqualTo(errorMessage));
        }

        [Test]
        public void ネットワーク関連例外がシリアライズ可能であること()
        {
            // Arrange & Act & Assert
            Assert.That(typeof(NetworkExceptionService).IsSerializable);
            Assert.That(typeof(VersionMismatchException).IsSerializable);
            Assert.That(typeof(MasterDataMismatchException).IsSerializable);
            Assert.That(typeof(DuplicateRequestException).IsSerializable);
        }
    }
}