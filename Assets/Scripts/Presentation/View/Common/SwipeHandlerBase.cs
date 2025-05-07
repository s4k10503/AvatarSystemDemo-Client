using UnityEngine;
using UnityEngine.UIElements;
using System;
using R3;
using Presentation.Interfaces;

namespace Presentation.View
{
    /// <summary>
    /// UIElementsでのスワイプ処理を行う基底クラス
    /// </summary>
    public abstract class SwipeHandlerBase : IDisposable
    {
        protected VisualElement _targetElement;
        protected Vector2 _touchStartPosition;
        protected float _currentPosition = 0f;
        protected const float OverscrollAmount = 50f;
        protected const float SwipeThreshold = 100f;

        protected CompositeDisposable _disposables = new();

        public int CurrentPage { get; protected set; }
        public Observable<int> OnPageChanged => _pageChangedSubject;
        public bool IsSwipeActive { get; private set; }

        private IInputHandler _inputHandler;
        public IInputHandler InputHandler
        {
            get => _inputHandler;
            set
            {
                if (_inputHandler == value) return;
                _inputHandler = value;
                SubscribeToInputHandlerEvents();
            }
        }

        protected readonly Subject<int> _pageChangedSubject = new();

        /// <summary>
        /// スワイプ対象のUIElementを設定
        /// </summary>
        /// <param name="element">UIElement</param>
        public void SetTargetElement(VisualElement element)
        {
            _targetElement = element;
        }

        /// <summary>
        /// 状態をリセット
        /// </summary>
        public virtual void Reset()
        {
            _touchStartPosition = Vector2.zero;
            _currentPosition = 0f;
            CurrentPage = 0;
            IsSwipeActive = false;
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();
            if (_targetElement != null)
            {
                UpdateElementPosition();
            }
        }

        /// <summary>
        /// 指定したページに切り替え
        /// </summary>
        /// <param name="page">ページ</param>
        public virtual void SwitchToPage(int page)
        {
            CurrentPage = page;
            _pageChangedSubject.OnNext(page);
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public virtual void Dispose()
        {
            _pageChangedSubject.Dispose();
            _disposables.Dispose();
        }

        /// <summary>
        /// InputHandlerのイベントを購読
        /// </summary>
        protected virtual void SubscribeToInputHandlerEvents()
        {
            _disposables.Clear();

            if (_inputHandler == null) return;

            _inputHandler.ElementDragStarted
                .Where(element => element == _targetElement)
                .Subscribe(element =>
                {
                    IsSwipeActive = true;
                    _touchStartPosition = _inputHandler.CurrentPointerPosition.CurrentValue;
                })
                .AddTo(_disposables);

            _inputHandler.ElementDragEnded
                .Where(element => element == _targetElement)
                .Subscribe(element =>
                {
                    if (IsSwipeActive)
                    {
                        HandleSwipeEnd(_inputHandler.CurrentPointerPosition.CurrentValue);
                    }
                    IsSwipeActive = false;
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// ポインターダウンイベントハンドラ
        /// </summary>
        /// <param name="evt">ポインターダウンイベント</param>
        public virtual void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.target is Slider)
            {
                evt.StopPropagation();
                return;
            }

            if (evt.target is Button) return;
            if (InputHandler == null || _targetElement == null) return;

            if (evt.target != _targetElement && !_targetElement.Contains(evt.target as VisualElement))
            {
                return;
            }

            InputHandler.StartDraggingOnElement(_targetElement);
            _targetElement?.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        /// <summary>
        /// ポインタームーブイベントハンドラ
        /// </summary>
        /// <param name="evt">ポインタームーブイベント</param>
        public abstract void OnPointerMove(PointerMoveEvent evt);

        /// <summary>
        /// ポインターアップイベントハンドラ
        /// </summary>
        /// <param name="evt">ポインターアップイベント</param>
        public virtual void OnPointerUp(PointerUpEvent evt)
        {
            if (InputHandler == null || _targetElement == null) return;

            if (_targetElement.HasPointerCapture(evt.pointerId))
            {
                _targetElement.ReleasePointer(evt.pointerId);
            }
            _touchStartPosition = Vector2.zero;
            evt.StopPropagation();
        }

        /// <summary>
        /// スワイプが終了したときの処理 (OnPointerUpまたはOnGlobalPointerUpから呼び出される)
        /// </summary>
        /// <param name="endPosition">スワイプが終了したポインターの位置。</param>
        protected virtual void HandleSwipeEnd(Vector2 endPosition)
        {
            UpdateElementPosition();
        }

        /// <summary>
        /// グローバルポインターアップイベント用ハンドラ
        /// 非UI領域でのポインターアップを処理
        /// </summary>
        /// <param name="position">位置</param>
        public virtual void OnGlobalPointerUp(Vector2 position)
        {
            if (IsSwipeActive)
            {
                IsSwipeActive = false;
                _touchStartPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// 要素の位置を更新
        /// </summary>
        protected abstract void UpdateElementPosition();
    }
}
