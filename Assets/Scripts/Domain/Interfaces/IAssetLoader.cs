using Cysharp.Threading.Tasks;
using System.Threading;
using Domain.ValueObjects; // For AssetLoadResult<T>
using UnityEngine; // For UnityEngine.Object constraint

namespace Domain.Interfaces
{
    /// <summary>
    /// アセットのロード/アンロードを行う汎用インターフェース
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// 指定されたアドレスからアセットを非同期にロードし、インスタンス化する。
        /// </summary>
        /// <typeparam name="T">ロードするアセットの型 (UnityEngine.Object を継承)。主に GameObject を想定。</typeparam>
        /// <param name="assetAddress">アセットのアドレスまたはID</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>ロード結果 (T型のインスタンスまたはエラー情報)</returns>
        UniTask<AssetLoadResult<T>> LoadAssetAsync<T>(string assetAddress, CancellationToken ct = default) where T : UnityEngine.Object;

        /// <summary>
        /// アセットインスタンスをアンロードする。
        /// </summary>
        /// <param name="assetInstance">アンロードするアセットのインスタンス (通常は GameObject)。</param>
        /// <returns>アンロード処理が試みられた場合はtrue、インスタンスが無効な場合はfalse。</returns>
        bool UnloadAsset(object assetInstance);
    }
}
