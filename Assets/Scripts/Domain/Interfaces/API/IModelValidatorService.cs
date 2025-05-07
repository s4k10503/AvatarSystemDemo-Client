
namespace Domain.Interfaces
{
    /// <summary>
    /// モデル検証サービスのインターフェース
    /// </summary>
    public interface IModelValidatorService
    {
        void Validate(object model);
    }
}
