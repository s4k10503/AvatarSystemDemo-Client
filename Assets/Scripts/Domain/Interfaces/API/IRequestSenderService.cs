using Cysharp.Threading.Tasks;
using System.Threading;
using Domain.ValueObjects;

namespace Domain.Interfaces
{
    /// <summary>
    /// REST通信を行う共通インターフェース
    /// </summary>
    public interface IRequestSenderService
    {
        /// <summary>
        /// REST APIリクエストを送信し、レスポンス文字列を返す。
        /// 例: GET, POST, PUT, PATCH, DELETE に対応。
        /// </summary>
        /// <param name="method">HTTPメソッド</param>
        /// <param name="url">URL</param>
        /// <param name="data">POST/PUT/PATCH等のボディデータ</param>
        /// <param name="maxRetries">最大リトライ回数</param>
        /// <param name="initialInterval">リトライ時の初期インターバル(秒)</param>
        /// <param name="timeoutSeconds">タイムアウト(秒)</param>
        /// <param name="cancellationToken">キャンセル用トークン</param>
        /// <param name="accessToken">APIへのアクセスに必要なトークン（必須）</param>
        /// <param name="externalRequestId">外部から指定されるリクエストID (オプショナル)</param>
        UniTask<string> SendRequestAsync(
            HttpMethod method,
            string url,
            object data = null,
            int maxRetries = 3,
            float initialInterval = 2.0f,
            float timeoutSeconds = 10.0f,
            CancellationToken cancellationToken = default,
            string accessToken = null,
            string externalRequestId = null
        );

        /// <summary>
        /// 進捗監視付きの大容量ダウンロード（GET想定）
        /// </summary>
        /// <param name="url">ダウンロード先URL</param>
        /// <param name="noProgressTimeout">進捗が変化しない場合のタイムアウト秒数</param>
        /// <param name="cancellationToken">キャンセル用トークン</param>
        UniTask DownloadWithProgressAsync(
            string url,
            float noProgressTimeout = 5.0f,
            CancellationToken cancellationToken = default
        );
    }
}
