using Domain.Interfaces;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using Domain.ValueObjects;

namespace Infrastructure.Services
{
    /// <summary>
    /// アバターのロードを行う
    /// </summary>
    public class AddressableAvatarLoader : IAvatarLoader
    {
        /// <summary>
        /// アバターをロードする
        /// </summary>
        /// <param name="avatarId">アバターのID</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>ロード結果</returns>
        public async UniTask<DomainLoadResult> LoadAvatarAsync(
            string avatarId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(avatarId))
            {
                return DomainLoadResult.Failure("アバターIDはnullまたは空です。");
            }

            AsyncOperationHandle<GameObject> handle = default;
            try
            {
                handle = Addressables.InstantiateAsync(avatarId);
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var avatarInstance = handle.Result;
                    if (avatarInstance == null)
                    {
                        return DomainLoadResult.Failure($"ロード後にインスタンス化したアバターがnullです: {avatarId}");
                    }

                    // Animator の有無はここで確認するが、DomainLoadResult にはこの情報は含めない。
                    // ただし、Animator が必須であるというビジネスルールがあるなら、ここで失敗として扱う。
                    Animator animator = avatarInstance.GetComponent<Animator>();
                    if (animator == null)
                    {
                        Addressables.ReleaseInstance(avatarInstance);
                        return DomainLoadResult.Failure($"アバターID: {avatarId} は Animator コンポーネントが必要です。");
                    }
                    // 成功時は GameObject インスタンスを Payload として返す
                    return DomainLoadResult.Success(avatarInstance);
                }
                else if (handle.Status == AsyncOperationStatus.Failed)
                {
                    string error = handle.OperationException?.Message ?? $"アバターのロードに失敗しました ID: {avatarId} (OperationException が null です)";
                    return DomainLoadResult.Failure(error);
                }
                else
                {
                    return DomainLoadResult.Failure($"アバターのロード操作が予期せず終了しました ID: {avatarId} のステータス: {handle.Status}");
                }
            }
            catch (OperationCanceledException)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
            catch (Exception ex)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw new Exception($"AddressableAvatarLoader で ID {avatarId} で予期しないエラーが発生しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// アバターをアンロードする
        /// </summary>
        /// <param name="avatarInstance">アバターのインスタンス</param>
        /// <returns>アンロードが試みられた場合はtrue、インスタンスが無効な場合はfalse</returns>
        public bool UnloadAvatar(object avatarInstance)
        {
            if (avatarInstance is GameObject go)
            {
                if (go != null)
                {
                    // Addressables.ReleaseInstance は bool を返さないため、
                    // ここではインスタンスが存在し、解放処理を試みたことをもって成功と見なす。
                    // より厳密な解放確認が必要な場合、Addressables のハンドル管理などが必要になる。
                    Addressables.ReleaseInstance(go);
                    return true;
                }
            }
            return false;
        }
    }
}
