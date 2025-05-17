namespace Domain.ValueObjects
{
    public class DomainLoadResult
    {
        public object Payload { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        private DomainLoadResult(object payload, bool isSuccess, string errorMessage)
        {
            Payload = payload;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static DomainLoadResult Success(object payload)
        {
            return new DomainLoadResult(payload, true, null);
        }

        public static DomainLoadResult Failure(string errorMessage)
        {
            return new DomainLoadResult(null, false, errorMessage);
        }
    }
}
