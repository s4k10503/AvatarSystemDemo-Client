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
    /// Addressables を使用して汎用的なアセットのロード/アンロードを行うクラス
    /// </summary>
    public class AddressableAssetLoader : IAssetLoader
    {
        /// <summary>
        /// 指定されたアドレスからアセットを非同期にロードし、インスタンス化する。
        /// </summary>
        public async UniTask<AssetLoadResult<T>> LoadAssetAsync<T>(
            string assetAddress, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetAddress))
            {
                return AssetLoadResult<T>.Failure("アセットアドレスはnullまたは空です。");
            }

            // 主に GameObject のインスタンス化を想定しているため、InstantiateAsync を使用。
            // もしアセットそのもの（ScriptableObjectなど）をロードしたい場合は、
            // Addressables.LoadAssetAsync<T>(assetAddress) を使用し、結果の型を T にキャストする。
            AsyncOperationHandle<GameObject> handle = default;
            try
            {
                // TがGameObjectであることを期待してInstantiateAsyncを使用する。
                // より厳密には、TがGameObjectでない場合のフォールバックやエラー処理が必要になるが、
                // 今回のユースケース (アバター、ステージ) ではGameObjectで問題ないと想定。
                handle = Addressables.InstantiateAsync(assetAddress);
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject instance = handle.Result;
                    if (instance == null)
                    {
                        return AssetLoadResult<T>.Failure($"ロード後にインスタンス化したアセットがnullです: {assetAddress}");
                    }

                    // T が GameObject またはその派生型であることを期待。
                    // もし T が Component の場合は GetComponent<T>() が必要。
                    if (instance is T typedInstance)
                    {
                        return AssetLoadResult<T>.Success(typedInstance);
                    }
                    else if (typeof(T).IsSubclassOf(typeof(Component)) || typeof(T) == typeof(Component))
                    {
                        // T が Component の場合、インスタンスから GetComponent<T> を試みる
                        var component = instance.GetComponent<T>();
                        if (component != null)
                        {
                            return AssetLoadResult<T>.Success(component);
                        }
                        else
                        {
                            Addressables.ReleaseInstance(instance); // 不要なインスタンスを解放
                            return AssetLoadResult<T>.Failure($"インスタンス化されたアセットから型 '{typeof(T).Name}' のコンポーネントを取得できませんでした: {assetAddress}");
                        }
                    }
                    else
                    {
                        // インスタンス化は成功したが、期待する型 T ではない場合
                        // (例: TがScriptableObjectだがInstantiateAsyncを使った場合など。この設計では通常発生しづらい)
                        Addressables.ReleaseInstance(instance); // 不要なインスタンスを解放
                        return AssetLoadResult<T>.Failure($"インスタンス化されたアセットの型が期待する型 '{typeof(T).Name}' と異なります: {assetAddress}");
                    }
                }
                else if (handle.Status == AsyncOperationStatus.Failed)
                {
                    string error = handle.OperationException?.Message ?? $"アセットのロードに失敗しました ID: {assetAddress} (OperationException が null です)";
                    return AssetLoadResult<T>.Failure(error);
                }
                else
                {
                    return AssetLoadResult<T>.Failure($"アセットのロード操作が予期せず終了しました ID: {assetAddress} のステータス: {handle.Status}");
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
                throw new Exception($"AddressableAssetLoader で ID {assetAddress} のロード中に予期しないエラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// アセットインスタンスをアンロードする。
        /// </summary>
        /// <param name="assetInstance">アンロードするアセットのインスタンス (通常は GameObject)。</param>
        /// <returns>アンロード処理が試みられた場合はtrue、インスタンスが無効な場合はfalse。</returns>
        public bool UnloadAsset(object assetInstance)
        {
            if (assetInstance is GameObject go)
            {
                if (go != null)
                {
                    Addressables.ReleaseInstance(go);
                    return true;
                }
            }
            else if (assetInstance is Component componentInstance)
            {
                // コンポーネントの場合、そのGameObjectを解放する
                if (componentInstance != null && componentInstance.gameObject != null)
                {
                    Addressables.ReleaseInstance(componentInstance.gameObject);
                    return true;
                }
            }
            // 他のUnityEngine.Object派生型で、Addressables経由でロードされたがInstantiateではないもの
            // (例: Addressables.LoadAssetAsync<ScriptableObject>) の解放は Addressables.Release(handleOrObject) を使う。
            // この汎用ローダーでは主にInstantiateAsyncを扱うため、GameObjectの解放に主眼を置いている。
            // より広範なアセットタイプに対応するには、解放戦略の調整が必要。

            return false;
        }
    }
}
