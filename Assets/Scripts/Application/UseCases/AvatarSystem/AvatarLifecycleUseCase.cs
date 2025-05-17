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
        private readonly IAvatarLoader _avatarLoader;
        private readonly ILogService _logService;
        private CancellationTokenSource _cts;

        /// <summary>
        /// AvatarLifecycleUseCaseのコンストラクタ
        /// </summary>
        /// <param name="avatarLoader"></param>
        /// <param name="logService"></param>
        public AvatarLifecycleUseCase(IAvatarLoader avatarLoader, ILogService logService)
        {
            _avatarLoader = avatarLoader;
            _logService = logService;
        }

        /// <summary>
        /// アバターをロードして初期セットアップを行う
        /// </summary>
        /// <param name="avatarId">アバターのID</param>
        /// <returns>ロードされたアバターのインスタンス</returns>
        public async UniTask<AvatarLoadResultDto> LoadAndSetupAvatarAsync(string avatarId)
        {
            // 既存のCTSがあればキャンセルして新しいものを作成
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            DomainLoadResult domainResult;
            try
            {
                domainResult = await _avatarLoader.LoadAvatarAsync(avatarId, ct);
            }
            catch (OperationCanceledException)
            {
                _logService.Info($"アバターのロードが ID: {avatarId} でキャンセルされました (CancellationTokenSource 経由)。");
                throw;
            }
            catch (Exception ex)
            {
                _logService.Error($"IAvatarLoader で ID {avatarId} で予期しないエラーが発生しました。", ex);
                throw;
            }

            if (!domainResult.IsSuccess)
            {
                _logService.Error($"アバター (domain) のロードに失敗しました ID: {avatarId}. 理由: {domainResult.ErrorMessage}");
                return new AvatarLoadResultDto(null, false, false, domainResult.ErrorMessage);
            }

            if (domainResult.Payload is not GameObject go)
            {
                _logService.Error($"アバター (domain) が正常にロードされました ID: {avatarId} ですが、ペイロードが GameObject ではありません。");
                return new AvatarLoadResultDto(domainResult.Payload, false, false, "ロードされたペイロードが有効な GameObject ではありません。");
            }

            bool hasAnimator = go.GetComponent<Animator>() != null;

            if (!hasAnimator && true)
            {
                _logService.Warning($"アバター ID: {avatarId} がロードされましたが、UseCaseでGetComponent<Animator>() が失敗しました。これは、ローダーが Animator を保証する場合には予期しないことです。");
            }

            _logService.Info($"アバター (GameObject) が正常に処理されました ID: {avatarId}. HasAnimator: {hasAnimator}");
            return new AvatarLoadResultDto(go, hasAnimator, true, null);
        }

        /// <summary>
        /// アバターを解放する
        /// </summary>
        /// <param name="avatarInstance">解放するアバターのインスタンス。</param>
        public void ReleaseAvatar(object avatarInstance)
        {
            if (avatarInstance == null)
            {
                _logService.Warning("解放試行: アバターインスタンスがnullです。提供されませんでした。");
                return;
            }

            string avatarIdentifier = "不明なアバター";
            if (avatarInstance is GameObject go)
            {
                avatarIdentifier = $"GameObject '{go.name}' (InstanceID: {go.GetInstanceID()})";
            }
            else
            {
                // 他の型の場合、ToString() を使用するか、型情報を含める
                avatarIdentifier = $"Instance of type '{avatarInstance.GetType().Name}' (ToString: {avatarInstance.ToString()})";
            }

            try
            {
                bool unloadedSuccessfully = _avatarLoader.UnloadAvatar(avatarInstance);
                if (unloadedSuccessfully)
                {
                    _logService.Info($"アバター ({avatarIdentifier}) の解放処理が正常に実行されました。");
                }
                else
                {
                    _logService.Warning($"アバター ({avatarIdentifier}) の解放処理は実行されましたが、ローダーは成功を示しませんでした (またはインスタンスが無効でした)。");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"アバター ({avatarIdentifier}) の解放中に予期せぬエラーが発生しました。", ex);
            }
        }

        /// <summary>
        /// 現在進行中のアバターロード処理があればキャンセルします。
        /// </summary>
        public void CancelAvatarLoading()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _logService.Info("アバターのロードをキャンセルします。");
                _cts.Cancel();
            }
        }

        /// <summary>
        /// リソースを解放する
        /// </summary>
        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
