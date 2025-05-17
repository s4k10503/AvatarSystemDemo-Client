using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Domain.Interfaces;
using Domain.ValueObjects;
using Application.DTO;
using UnityEngine;

namespace Application.UseCases
{
    public class AvatarLifecycleUseCase : IDisposable
    {
        private readonly IAssetLoader _assetLoader;
        private readonly ILogService _logService;
        private CancellationTokenSource _avatarLoadingCts;
        private CancellationTokenSource _stageLoadingCts;

        /// <summary>
        /// AvatarLifecycleUseCaseのコンストラクタ
        /// </summary>
        /// <param name="assetLoader"></param>
        /// <param name="logService"></param>
        public AvatarLifecycleUseCase(IAssetLoader assetLoader, ILogService logService)
        {
            _assetLoader = assetLoader;
            _logService = logService;
        }

        /// <summary>
        /// アバターをロードして初期セットアップを行う
        /// </summary>
        /// <param name="avatarAddress">アバターのアドレス</param>
        /// <returns>ロードされたアバターのインスタンス情報</returns>
        public async UniTask<AvatarLoadResultDto> LoadAndSetupAvatarAsync(string avatarAddress)
        {
            _avatarLoadingCts?.Cancel();
            _avatarLoadingCts?.Dispose();
            _avatarLoadingCts = new CancellationTokenSource();
            var ct = _avatarLoadingCts.Token;

            AssetLoadResult<GameObject> loadResult;
            try
            {
                loadResult = await _assetLoader.LoadAssetAsync<GameObject>(avatarAddress, ct);
            }
            catch (OperationCanceledException)
            {
                _logService.Info($"アバターのロードが ID: {avatarAddress} でキャンセルされました (CancellationTokenSource 経由)。");
                throw;
            }
            catch (Exception ex)
            {
                _logService.Error($"IAssetLoader でアバター ID {avatarAddress} のロード中に予期しないエラー。", ex);
                throw;
            }

            if (!loadResult.IsSuccess)
            {
                _logService.Error($"アバター (AssetLoadResult) のロードに失敗 ID: {avatarAddress}. 理由: {loadResult.ErrorMessage}");
                return new AvatarLoadResultDto(null, false, false, loadResult.ErrorMessage);
            }

            GameObject avatarInstance = loadResult.Payload;
            if (avatarInstance == null)
            {
                _logService.Error($"アバター (AssetLoadResult) が正常にロードされました ID: {avatarAddress} ですが、ペイロードがnullです。");
                return new AvatarLoadResultDto(null, false, false, "ロードされたペイロードがnullです。");
            }

            bool hasAnimator = avatarInstance.GetComponent<Animator>() != null;
            if (!hasAnimator)
            {
                _logService.Error($"アバター ID: {avatarAddress} にはAnimatorコンポーネントが必須ですが、見つかりませんでした。アセットを解放します。");
                _assetLoader.UnloadAsset(avatarInstance);
                return new AvatarLoadResultDto(null, false, false, $"アバター ID: {avatarAddress} にAnimatorコンポーネントが見つかりません。");
            }

            _logService.Info($"アバターが正常に処理されました ID: {avatarAddress}. HasAnimator: {hasAnimator}");
            return new AvatarLoadResultDto(avatarInstance, hasAnimator, true, null);
        }

        /// <summary>
        /// ステージをロードしてセットアップする
        /// </summary>
        /// <param name="stageAddress">ステージのアドレス</param>
        /// <returns>ロードされたステージのインスタンス情報</returns>
        public async UniTask<StageLoadResultDto> LoadAndSetupStageAsync(string stageAddress)
        {
            _stageLoadingCts?.Cancel();
            _stageLoadingCts?.Dispose();
            _stageLoadingCts = new CancellationTokenSource();
            var ct = _stageLoadingCts.Token;

            AssetLoadResult<GameObject> loadResult;
            try
            {
                loadResult = await _assetLoader.LoadAssetAsync<GameObject>(stageAddress, ct);
            }
            catch (OperationCanceledException)
            {
                _logService.Info($"ステージのロードが ID: {stageAddress} でキャンセルされました。");
                throw;
            }
            catch (Exception ex)
            {
                _logService.Error($"IAssetLoader でステージ ID {stageAddress} のロード中に予期しないエラー。", ex);
                throw;
            }

            if (!loadResult.IsSuccess)
            {
                _logService.Error($"ステージ (AssetLoadResult) のロードに失敗 ID: {stageAddress}. 理由: {loadResult.ErrorMessage}");
                return new StageLoadResultDto(null, false, loadResult.ErrorMessage);
            }

            GameObject stageInstance = loadResult.Payload;
            if (stageInstance == null)
            {
                _logService.Error($"ステージ (AssetLoadResult) が正常にロードされました ID: {stageAddress} ですが、ペイロードがnullです。");
                return new StageLoadResultDto(null, false, "ロードされたステージペイロードがnullです。");
            }

            _logService.Info($"ステージが正常に処理されました ID: {stageAddress}.");
            return new StageLoadResultDto(stageInstance, true, null);
        }

        /// <summary>
        /// アバターインスタンスを解放する
        /// </summary>
        public void ReleaseAvatar(object avatarInstance)
        {
            ReleaseAssetInternal(avatarInstance, "アバター");
        }

        /// <summary>
        /// ステージインスタンスを解放する
        /// </summary>
        public void ReleaseStage(object stageInstance)
        {
            ReleaseAssetInternal(stageInstance, "ステージ");
        }

        /// <summary>
        /// アセットを解放する内部メソッド
        /// </summary>
        /// <param name="assetInstance">解放するアセットのインスタンス</param>
        /// <param name="assetType">アセットの型 (例: "アバター", "ステージ")</param>
        private void ReleaseAssetInternal(object assetInstance, string assetType)
        {
            if (assetInstance == null)
            {
                _logService.Warning($"解放試行: {assetType}インスタンスがnullです。");
                return;
            }

            string identifier = "不明なアセット";
            if (assetInstance is GameObject go)
            {
                identifier = $"GameObject '{go.name}' (InstanceID: {go.GetInstanceID()})";
            }
            else if (assetInstance is Component component)
            {
                identifier = $"Component '{component.GetType().Name}' on GameObject '{component.gameObject.name}' (InstanceID: {component.gameObject.GetInstanceID()})";
            }
            else
            {
                identifier = $"Instance of type '{assetInstance.GetType().Name}'";
            }

            try
            {
                bool unloadedSuccessfully = _assetLoader.UnloadAsset(assetInstance);
                if (unloadedSuccessfully)
                {
                    _logService.Info($"{assetType} ({identifier}) の解放処理が正常に実行されました。");
                }
                else
                {
                    _logService.Warning($"{assetType} ({identifier}) の解放処理は実行されましたが、ローダーは成功を示しませんでした (またはインスタンスが無効でした)。");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"{assetType} ({identifier}) の解放中に予期せぬエラー。", ex);
            }
        }

        /// <summary>
        /// 現在進行中のアバターロード処理があればキャンセルします。
        /// </summary>
        public void CancelAvatarLoading()
        {
            if (_avatarLoadingCts != null && !_avatarLoadingCts.IsCancellationRequested)
            {
                _logService.Info("アバターのロードをキャンセルします。");
                _avatarLoadingCts.Cancel();
            }
        }

        /// <summary>
        /// 現在進行中のステージロード処理があればキャンセルします。
        /// </summary>
        public void CancelStageLoading()
        {
            if (_stageLoadingCts != null && !_stageLoadingCts.IsCancellationRequested)
            {
                _logService.Info("ステージのロードをキャンセルします。");
                _stageLoadingCts.Cancel();
            }
        }

        /// <summary>
        /// リソースを解放する
        /// </summary>
        public void Dispose()
        {
            _avatarLoadingCts?.Cancel();
            _avatarLoadingCts?.Dispose();
            _avatarLoadingCts = null;

            _stageLoadingCts?.Cancel();
            _stageLoadingCts?.Dispose();
            _stageLoadingCts = null;
        }
    }
}
