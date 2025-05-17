using UnityEngine;

namespace Application.DTO
{
    /// <summary>
    /// ステージロード処理の結果を保持するデータ転送オブジェクト。
    /// </summary>
    public class StageLoadResultDto
    {
        /// <summary>
        /// ロードされたステージのGameObjectインスタンス。
        /// 成功しなかった場合はnullの可能性があります。
        /// </summary>
        public GameObject StageInstance { get; }

        /// <summary>
        /// ロード処理が成功したかどうかを示します。
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// エラーが発生した場合のエラーメッセージ。
        /// 成功した場合はnullまたは空の可能性があります。
        /// </summary>
        public string ErrorMessage { get; }

        public StageLoadResultDto(GameObject stageInstance, bool isSuccess, string errorMessage)
        {
            StageInstance = stageInstance;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }
    }
}
