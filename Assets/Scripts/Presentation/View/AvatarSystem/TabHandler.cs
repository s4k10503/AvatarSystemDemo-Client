using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Presentation.View
{
    /// <summary>
    /// タブハンドラー
    /// </summary>
    /// <remarks>
    /// タブハンドラー
    /// </remarks>
    public sealed class TabHandler : SwipeHandlerBase
    {
        private float _maxScrollPosition = 0f;

        public void SetScrollContent(VisualElement scrollContent)
        {
            SetTargetElement(scrollContent);
        }

        /// <summary>
        /// ポインター移動
        /// </summary>
        /// <param name="evt">ポインター移動イベント</param>
        public override void OnPointerMove(PointerMoveEvent evt)
        {
            if (_touchStartPosition == Vector2.zero || !IsSwipeActive) return;
            if (_targetElement == null) return;

            if (evt.target is Slider)
            {
                evt.StopPropagation();
                return;
            }

            if (evt.target is Button) return;

            Vector2 currentPosition = evt.position;
            float swipeDistance = currentPosition.x - _touchStartPosition.x;

            var parent = _targetElement.parent;
            if (parent == null) return;

            float totalTabWidth = (parent.Q<Button>().resolvedStyle.width * 2) + 16f;
            float containerWidth = parent.resolvedStyle.width - 64f;
            _maxScrollPosition = Mathf.Max(0, totalTabWidth - containerWidth);

            float targetPosition = -_currentPosition + swipeDistance;

            // オーバースクロールの制限
            if (_currentPosition == 0)
            {
                targetPosition = Mathf.Min(targetPosition, OverscrollAmount);
            }
            else if (_currentPosition >= _maxScrollPosition)
            {
                targetPosition = Mathf.Max(targetPosition, -_maxScrollPosition - OverscrollAmount);
            }

            _targetElement.style.translate = new StyleTranslate(new Translate(targetPosition, 0, 0));
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles the logic when a swipe ends
        /// </summary>
        /// <param name="touchEndPosition">The pointer position where the swipe ended.</param>
        protected override void HandleSwipeEnd(Vector2 touchEndPosition)
        {
            float swipeDistance = touchEndPosition.x - _touchStartPosition.x;

            var parent = _targetElement.parent;
            if (parent == null) return;

            float totalTabWidth = (parent.Q<Button>().resolvedStyle.width * 2) + 16f;
            float containerWidth = parent.resolvedStyle.width - 64f;
            _maxScrollPosition = Mathf.Max(0, totalTabWidth - containerWidth);

            // スワイプ距離に基づいてスクロール位置を更新
            if (Mathf.Abs(swipeDistance) > SwipeThreshold)
            {
                if (swipeDistance > 0 && _currentPosition > 0)
                {
                    _currentPosition = Mathf.Max(0, _currentPosition - containerWidth);
                }
                else if (swipeDistance < 0 && _currentPosition < _maxScrollPosition)
                {
                    _currentPosition = Mathf.Min(_maxScrollPosition, _currentPosition + containerWidth);
                }
            }

            UpdateElementPosition();
        }

        /// <summary>
        /// 要素位置更新
        /// </summary>
        protected override void UpdateElementPosition()
        {
            if (_targetElement == null) return;
            _targetElement.style.transitionDuration = new List<TimeValue> { new TimeValue(0.3f, TimeUnit.Second) };
            _targetElement.style.translate = new StyleTranslate(new Translate(-_currentPosition, 0, 0));
        }
    }
}
