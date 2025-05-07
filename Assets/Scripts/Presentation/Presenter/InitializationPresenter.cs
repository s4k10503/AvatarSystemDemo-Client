using System;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using R3;

using Application.UseCases;
using Presentation.Interfaces;

namespace Presentation.Presenter
{
    /// <summary>
    /// 初期化Presenter
    /// </summary>
    /// <remarks>
    /// 初期化Presenter
    /// </remarks>
    public sealed class InitializationPresenter : IStartable, IDisposable
    {
        private readonly LoginUseCase _loginUseCase;
        private readonly IPageManager _pageManager;
        private readonly CompositeDisposable _disposables = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        [Inject]
        public InitializationPresenter(
            LoginUseCase loginUseCase,
            IPageManager pageManager)
        {
            _loginUseCase = loginUseCase;
            _pageManager = pageManager;
        }

        /// <summary>
        /// 開始時の処理
        /// </summary>
        public void Start()
        {
            Debug.Log("InitializationPresenter: Start Application");

            // アプリ初期化処理を実行
            InitializeApplicationAsync().Forget();
        }

        /// <summary>
        /// アプリ初期化処理
        /// </summary>
        private async UniTask InitializeApplicationAsync()
        {
            try
            {
                // ページの初期化
                _pageManager.InitializePages();

                // Firebaseの初期化
                await _loginUseCase.InitializeAsync();

                // 認証状態とトークンを取得
                bool isAuthenticated = _loginUseCase.IsAuthenticated();
                Debug.Log($"認証状態: {isAuthenticated}");

                if (isAuthenticated)
                {
                    try
                    {
                        string token = await _loginUseCase.GetAccessTokenAsync();
                        if (string.IsNullOrEmpty(token))
                        {
                            // トークンが空の場合は期限切れと判断
                            _pageManager.NavigateTo(PageType.Login);
                        }
                        else
                        {
                            // 認証済みかつトークンが有効な場合、アバターページに遷移
                            _pageManager.NavigateTo(PageType.AvatarSystem);
                        }
                    }
                    catch (Exception ex)
                    {
                        // トークン取得に失敗した場合（期限切れなど）
                        Debug.LogError($"トークンエラー: {ex.Message}");
                        _pageManager.NavigateTo(PageType.Login);
                    }
                }
                else
                {
                    // 未認証の場合はログインページを表示
                    _pageManager.NavigateTo(PageType.Login);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"初期化エラー: {ex.Message}");
                // エラーが発生した場合はログインページを表示
                _pageManager.NavigateTo(PageType.Login);
            }
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

