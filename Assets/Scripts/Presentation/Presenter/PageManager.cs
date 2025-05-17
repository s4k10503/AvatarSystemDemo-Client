using System.Collections.Generic;
using VContainer;
using UnityEngine;

using Presentation.Interfaces;
using Presentation.View;

namespace Presentation.Presenter
{
    /// <summary>
    /// ページマネージャー
    /// </summary>
    /// <remarks>
    /// ページマネージャー
    /// </remarks>
    public sealed class PageManager : IPageManager
    {
        private readonly Dictionary<PageType, GameObject> _pages = new();
        private PageType _currentPage = PageType.Login;
        private readonly LoginPage _loginPage;
        private readonly AvatarSystemPage _avatarSystemPage;
        private readonly RoomSystemPage _roomSystemPage;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="loginPage">ログインページ</param>
        /// <param name="avatarSystemPage">アバターシステムページ</param>
        /// <param name="roomSystemPage">ルームシステムページ</param>
        [Inject]
        public PageManager(
            LoginPage loginPage,
            AvatarSystemPage avatarSystemPage,
            RoomSystemPage roomSystemPage)
        {
            _loginPage = loginPage;
            _avatarSystemPage = avatarSystemPage;
            _roomSystemPage = roomSystemPage;
        }

        /// <summary>
        /// ページの初期化と登録
        /// </summary>
        public void InitializePages()
        {
            Debug.Log("PageManager: Initializing pages");

            // 各ページを登録
            RegisterPage(PageType.Login, _loginPage.gameObject);
            RegisterPage(PageType.AvatarSystem, _avatarSystemPage.gameObject);
            RegisterPage(PageType.RoomSystem, _roomSystemPage.gameObject);
        }

        /// <summary>
        /// ページの登録
        /// </summary>
        /// <param name="pageType">ページタイプ</param>
        /// <param name="page">ページオブジェクト</param>
        public void RegisterPage(PageType pageType, GameObject page)
        {
            if (!_pages.ContainsKey(pageType))
            {
                _pages.Add(pageType, page);
                page.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"ページタイプ {pageType} は既に登録されています。");
            }
        }

        /// <summary>
        /// ページ遷移
        /// </summary>
        /// <param name="pageType">ページタイプ</param>
        public void NavigateTo(PageType pageType)
        {
            Debug.Log($"NavigateTo が呼び出されました: {pageType}, 現在のページ: {_currentPage}");

            // 新しいページが登録されているか確認
            if (!_pages.ContainsKey(pageType))
            {
                Debug.LogError($"指定されたページタイプが見つかりません: {pageType}");
                return;
            }

            // 現在のページと同じでも、状態を確認して必要なら再アクティブ化
            if (_currentPage == pageType)
            {
                // 既に正しく表示されているなら何もしない
                if (_pages[pageType].activeSelf)
                {
                    Debug.Log($"ページ {pageType} は既にアクティブです");
                    return;
                }

                // 同じページでも非アクティブならアクティブにする
                _pages[pageType].SetActive(true);
                Debug.Log($"同じページ {pageType} を再アクティブ化しました");
                return;
            }

            // 現在のページを非表示にする
            if (_pages.ContainsKey(_currentPage))
            {
                _pages[_currentPage].SetActive(false);
                Debug.Log($"ページ {_currentPage} を非アクティブ化しました");
            }

            // 新しいページを表示する
            _pages[pageType].SetActive(true);
            _currentPage = pageType;
            Debug.Log($"ページ遷移完了: {pageType}");
        }

        /// <summary>
        /// 現在のページを取得
        /// </summary>
        public PageType GetCurrentPage()
        {
            return _currentPage;
        }
    }
}
