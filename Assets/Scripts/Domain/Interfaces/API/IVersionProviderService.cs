namespace Domain.Interfaces
{

    /// <summary>
    /// バージョン情報提供のインターフェース
    /// </summary>
    public interface IVersionProviderService
    {
        string AppVersion { get; }
        string MasterDataVersion { get; }
    }
}
