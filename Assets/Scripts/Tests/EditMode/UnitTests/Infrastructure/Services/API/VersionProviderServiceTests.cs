using NUnit.Framework;
using Domain.ValueObjects;
using Infrastructure.Services;

namespace UnitTests.Infrastructure
{
    public class VersionProviderServiceTests
    {
        private VersionProviderService _versionProviderService;
        private ApiConnections _apiConnections;

        [SetUp]
        public void Setup()
        {
            _apiConnections = new ApiConnections(
                baseUrl: "https://api.example.com",
                maxRetries: 3,
                initialInterval: 2.0f,
                timeoutSeconds: 30,
                appVersion: "1.0.0",
                masterDataVersion: "2.0.0"
            );

            _versionProviderService = new VersionProviderService(_apiConnections);
        }

        [Test]
        public void アプリバージョン_正しいバージョンが返されること()
        {
            // Arrange
            const string expectedVersion = "1.0.0";

            // Act
            string actualVersion = _versionProviderService.AppVersion;

            // Assert
            Assert.That(actualVersion, Is.EqualTo(expectedVersion));
        }

        [Test]
        public void マスターデータバージョン_正しいバージョンが返されること()
        {
            // Arrange
            const string expectedVersion = "2.0.0";

            // Act
            string actualVersion = _versionProviderService.MasterDataVersion;

            // Assert
            Assert.That(actualVersion, Is.EqualTo(expectedVersion));
        }
    }
}
