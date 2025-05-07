using Domain.Interfaces;
using Domain.ValueObjects;

namespace Infrastructure.Services
{
    /// <summary>
    /// バージョン情報を提供するサービス
    /// </summary>
    public sealed class VersionProviderService : IVersionProviderService
    {
        private readonly ApiConnections _apiConnections;

        public VersionProviderService(ApiConnections apiConnections)
        {
            _apiConnections = apiConnections;
        }

        public string AppVersion => _apiConnections.AppVersion;
        public string MasterDataVersion => _apiConnections.MasterDataVersion;
    }
}
