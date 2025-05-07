using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Presentation.View
{
    /// <summary>
    /// ページコンテンツハンドラー
    /// </summary>
    /// <remarks>
    /// ページコンテンツハンドラー
    /// </remarks>
    public sealed class PageContentHandler : SwipeHandlerBase
    {
        private int _maxPages = 2; // Default to 2 pages

        /// <summary>
        /// コンテンツコンテナを設定
        /// </summary>
        /// <param name="container">コンテンツコンテナ</param>
        public void SetContentContainer(VisualElement container)
        {
            SetTargetElement(container);
        }

        /// <summary>
        /// ページ数を設定
        /// </summary>
        /// <param name="maxPages">ページ数</param>
        public void SetMaxPages(int maxPages)
        {
            _maxPages = Mathf.Max(1, maxPages); // 少なくとも1ページ
        }

        /// <summary>
        /// リセット
        /// </summary>
        public override void Reset()
        {
            base.Reset();
        }

        /// <summary>
        /// ポインター移動
        /// </summary>
        /// <param name="evt">ポインター移動イベント</param>
        public override void OnPointerMove(PointerMoveEvent evt)
        {
            if (_touchStartPosition == Vector2.zero || !IsSwipeActive) return;
            if (_targetElement == null || _targetElement.parent == null) return;

            if (evt.target is Slider || evt.target is Button)
            {
                evt.StopPropagation();
                return;
            }

            Vector2 currentPosition = evt.position;
            float swipeDistance = currentPosition.x - _touchStartPosition.x;
            float pageWidth = _targetElement.parent.resolvedStyle.width;
            float targetPosition = -CurrentPage * pageWidth + swipeDistance;

            if (CurrentPage == 0)
            {
                targetPosition = Mathf.Min(targetPosition, OverscrollAmount);
            }
            else if (CurrentPage == _maxPages - 1)
            {
                targetPosition = Mathf.Max(targetPosition, -(CurrentPage * pageWidth) - OverscrollAmount);
            }

            _targetElement.style.translate = new StyleTranslate(new Translate(targetPosition, 0, 0));
        }

        /// <summary>
        /// スワイプが終了したときの処理
        /// </summary>
        /// <param name="touchEndPosition">スワイプが終了したポインターの位置。</param>
        protected override void HandleSwipeEnd(Vector2 touchEndPosition)
        {
            float swipeDistance = touchEndPosition.x - _touchStartPosition.x;
            float pageWidth = _targetElement.parent.resolvedStyle.width;

            int newPage = CurrentPage;
            float swipeThreshold = SwipeThreshold;

            if (Mathf.Abs(swipeDistance) > swipeThreshold)
            {
                if (swipeDistance > 0 && CurrentPage > 0)
                {
                    newPage--;
                }
                else if (swipeDistance < 0 && CurrentPage < _maxPages - 1)
                {
                    newPage++;
                }
            }

            if (newPage != CurrentPage)
            {
                CurrentPage = newPage;
                _pageChangedSubject.OnNext(CurrentPage);
            }

            UpdateElementPosition();
        }

        /// <summary>
        /// 要素位置更新
        /// </summary>
        protected override void UpdateElementPosition()
        {
            if (_targetElement == null || _targetElement.parent == null) return;
            float pageWidth = _targetElement.parent.resolvedStyle.width;
            float targetPosition = -CurrentPage * pageWidth;
            _targetElement.style.transitionDuration = new List<TimeValue> { new(0.3f, TimeUnit.Second) };
            _targetElement.style.translate = new StyleTranslate(new Translate(targetPosition, 0, 0));
        }

        /// <summary>
        /// ページ切り替え
        /// </summary>
        /// <param name="page">ページ</param>
        public override void SwitchToPage(int page)
        {
            base.SwitchToPage(page);
            UpdateElementPosition();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
