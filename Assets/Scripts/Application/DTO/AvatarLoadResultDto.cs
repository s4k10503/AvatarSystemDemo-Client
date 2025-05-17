namespace Application.DTO
{
    public class AvatarLoadResultDto
    {
        public object AvatarInstance { get; } // Presenter で GameObject にキャスト想定
        public bool HasAnimator { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public AvatarLoadResultDto(
            object avatarInstance, bool hasAnimator, bool isSuccess, string errorMessage = null)
        {
            AvatarInstance = avatarInstance;
            HasAnimator = hasAnimator;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        // 失敗用のファクトリメソッド
        public static AvatarLoadResultDto Failure(
            string errorMessage, object avatarInstance = null, bool hasAnimator = false)
        {
            return new AvatarLoadResultDto(avatarInstance, hasAnimator, false, errorMessage);
        }
    }
}
