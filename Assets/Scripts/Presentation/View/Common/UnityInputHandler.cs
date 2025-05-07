using UnityEngine;
using UnityEngine.UIElements;
using Presentation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using R3;

namespace Presentation.View
{
    /// <summary>
    /// Unityの入力ハンドラー
    /// </summary>
    /// <remarks>
    /// Unityの入力ハンドラー
    /// </remarks>
    public sealed class UnityInputHandler : IInputHandler, IDisposable
    {
        private readonly UIDocument _uiDocument;
        private bool _isDragging_Internal;
        private Vector2 _dragStartPosition_Internal;
        public const float SHORT_DRAG_THRESHOLD = 10f; // タップとみなす距離のしきい値(px)

        // ドラッグを開始した要素を追跡
        private VisualElement _draggingElement_Internal;
        public VisualElement DraggingElement => _draggingElement_Internal;

        // --- R3 Subjects and Properties ---
        private readonly Subject<Vector2> _pointerDownSubject = new();
        private readonly Subject<Vector2> _pointerUpSubject = new();
        private readonly Subject<Vector2> _pointerMoveSubject = new();
        private readonly ReactiveProperty<Vector2> _currentPointerPosition = new(Vector2.zero);
        private readonly ReactiveProperty<Vector2> _previousPointerPosition = new(Vector2.zero);
        private readonly ReactiveProperty<bool> _isDragging = new(false);
        private readonly ReactiveProperty<float> _dragDistance = new(0f);
        private readonly Subject<Unit> _globalDragStartedSubject = new();
        private readonly Subject<Unit> _globalDragEndedSubject = new();
        private readonly Subject<VisualElement> _elementDragStartedSubject = new();
        private readonly Subject<VisualElement> _elementDragEndedSubject = new();

        private readonly CompositeDisposable _disposables = new();

        // --- R3 Observables --- (Public Accessors)
        public Observable<Vector2> PointerDown => _pointerDownSubject;
        public Observable<Vector2> PointerUp => _pointerUpSubject;
        public Observable<Vector2> PointerMove => _pointerMoveSubject;
        public ReadOnlyReactiveProperty<Vector2> CurrentPointerPosition => _currentPointerPosition;
        public ReadOnlyReactiveProperty<Vector2> PreviousPointerPosition => _previousPointerPosition;
        public ReadOnlyReactiveProperty<bool> IsDragging => _isDragging;
        public ReadOnlyReactiveProperty<float> DragDistance => _dragDistance;
        public Observable<Unit> GlobalDragStarted => _globalDragStartedSubject;
        public Observable<Unit> GlobalDragEnded => _globalDragEndedSubject;
        public Observable<VisualElement> ElementDragStarted => _elementDragStartedSubject;
        public Observable<VisualElement> ElementDragEnded => _elementDragEndedSubject;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="uiDocument">UIドキュメント</param>
        public UnityInputHandler(UIDocument uiDocument)
        {
            _uiDocument = uiDocument;

            // Start the update loop
            Observable.EveryUpdate()
                .Subscribe(_ => UpdateInputState())
                .AddTo(_disposables);
        }

        /// <summary>
        /// 毎フレームの入力状態をチェックし、R3イベントを発行/プロパティを更新
        /// </summary>
        private void UpdateInputState()
        {
            Vector2 currentPosition = GetCurrentRawPointerPosition();
            _previousPointerPosition.Value = _currentPointerPosition.Value;
            _currentPointerPosition.Value = currentPosition;

            // --- Pointer Down ---
            bool pointerDownThisFrame = false;
            Vector2 downPosition = Vector2.zero;
            Touch touchBegan = default;
            if (Input.touchCount > 0 && TryGetTouchByPhase(TouchPhase.Began, out touchBegan))
            {
                downPosition = touchBegan.position;
                pointerDownThisFrame = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                downPosition = Input.mousePosition;
                pointerDownThisFrame = true;
            }
            if (pointerDownThisFrame)
            {
                _pointerDownSubject.OnNext(downPosition);
                if (!IsPointerOverUI(downPosition) && _draggingElement_Internal == null)
                {
                    StartGlobalDraggingInternal(downPosition);
                }
            }

            // --- Pointer Up ---
            bool pointerUpThisFrame = false;
            Vector2 upPosition = Vector2.zero;
            if (Input.touchCount > 0)
            {
                foreach (var touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        upPosition = touch.position;
                        pointerUpThisFrame = true;
                        break;
                    }
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (!pointerUpThisFrame)
                {
                    upPosition = Input.mousePosition;
                }
                pointerUpThisFrame = true;
            }

            if (pointerUpThisFrame)
            {
                _pointerUpSubject.OnNext(upPosition);
                if (_isDragging_Internal)
                {
                    if (_draggingElement_Internal != null)
                    {
                        StopDraggingOnElementInternal(_draggingElement_Internal);
                    }
                    else
                    {
                        StopGlobalDraggingInternal();
                    }
                }
            }

            // --- Pointer Move ---
            bool pointerMovedThisFrame = false;
            Vector2 movePosition = Vector2.zero;
            if (Input.touchCount > 0 && TryGetTouchByPhase(TouchPhase.Moved, out Touch touchMoved))
            {
                movePosition = touchMoved.position;
                pointerMovedThisFrame = true;
            }
            else if (Input.GetMouseButton(0))
            {
                movePosition = Input.mousePosition;
                if (Vector2.Distance(movePosition, _previousPointerPosition.Value) > 0.1f)
                {
                    pointerMovedThisFrame = true;
                }
            }

            if (pointerMovedThisFrame)
            {
                _pointerMoveSubject.OnNext(movePosition);
            }

            // --- Update Drag Distance ---
            if (_isDragging_Internal)
            {
                _dragDistance.Value = Vector2.Distance(_dragStartPosition_Internal, currentPosition);
            }
            else
            {
                if (_dragDistance.Value != 0f)
                {
                    _dragDistance.Value = 0f;
                }
            }
        }

        /// <summary>
        /// 現在の生のポインター位置を取得 (タッチ優先)
        /// </summary>
        private Vector2 GetCurrentRawPointerPosition()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }
            return Input.mousePosition;
        }

        /// <summary>
        /// ポインターがUI上にあるかどうか
        /// </summary>
        /// <param name="position">位置</param>
        public bool IsPointerOverUI(Vector2 position)
        {
            if (_uiDocument?.rootVisualElement == null) return false;
            Vector2 screenPosition = new(position.x, Screen.height - position.y);
            return _uiDocument.rootVisualElement.panel.Pick(screenPosition) != null;
        }

        /// <summary>
        /// ドラッグ開始 (特定の要素から呼び出される)
        /// </summary>
        /// <param name="element">ドラッグを開始した要素</param>
        public void StartDraggingOnElement(VisualElement element)
        {
            StartDraggingOnElementInternal(element, _currentPointerPosition.Value);
        }

        /// <summary>
        /// グローバルなドラッグを開始 (UI要素に紐付かない)
        /// </summary>
        public void StartGlobalDragging()
        {
            StartGlobalDraggingInternal(_currentPointerPosition.Value);
        }

        /// <summary>
        /// グローバルなドラッグを終了
        /// </summary>
        private void StopGlobalDraggingInternal()
        {
            if (!_isDragging_Internal || _draggingElement_Internal != null) return;

            _isDragging_Internal = false;
            _isDragging.Value = false;
            _globalDragEndedSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// 現在のタッチ数を取得します。
        /// </summary>
        /// <returns>タッチ数</returns>
        public int GetTouchCount()
        {
            return Input.touchCount;
        }

        /// <summary>
        /// 指定したインデックスのタッチ情報を取得します。
        /// </summary>
        /// <param name="index">タッチのインデックス</param>
        /// <returns>タッチ情報。存在しない場合はnull</returns>
        public Touch? GetTouch(int index)
        {
            if (index < 0 || index >= Input.touchCount)
            {
                return null;
            }
            return Input.GetTouch(index);
        }

        /// <summary>
        /// 指定したフェーズに合致する *すべての* タッチを取得します。
        /// </summary>
        /// <param name="phase">検索するタッチフェーズ</param>
        /// <returns>合致するタッチ情報の列挙子</returns>
        public IEnumerable<Touch> GetTouchesByPhase(TouchPhase phase)
        {
            return Input.touches.Where(t => t.phase == phase);
        }

        /// <summary>
        /// リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            _pointerDownSubject.Dispose();
            _pointerUpSubject.Dispose();
            _pointerMoveSubject.Dispose();
            _currentPointerPosition.Dispose();
            _previousPointerPosition.Dispose();
            _isDragging.Dispose();
            _dragDistance.Dispose();
            _globalDragStartedSubject.Dispose();
            _globalDragEndedSubject.Dispose();
            _elementDragStartedSubject.Dispose();
            _elementDragEndedSubject.Dispose();
            _disposables.Dispose();
        }

        // --- Internal Drag Logic ---

        /// <summary>
        /// 要素のドラッグを開始します。
        /// </summary>
        /// <param name="element">ドラッグを開始する要素</param>
        /// <param name="startPosition">ドラッグ開始位置</param>
        private void StartDraggingOnElementInternal(VisualElement element, Vector2 startPosition)
        {
            if (_isDragging_Internal) return;

            _isDragging_Internal = true;
            _draggingElement_Internal = element;
            _dragStartPosition_Internal = startPosition;
            _isDragging.Value = true;
            _dragDistance.Value = 0f;
            _elementDragStartedSubject.OnNext(element);
        }

        /// <summary>
        /// 要素のドラッグを停止します。
        /// </summary>
        /// <param name="element">ドラッグを停止する要素</param>
        private void StopDraggingOnElementInternal(VisualElement element)
        {
            if (!_isDragging_Internal || _draggingElement_Internal != element) return;

            var endedElement = _draggingElement_Internal;
            _isDragging_Internal = false;
            _draggingElement_Internal = null;
            _isDragging.Value = false;
            _elementDragEndedSubject.OnNext(endedElement);
        }

        /// <summary>
        /// グローバルなドラッグを開始します。
        /// </summary>
        /// <param name="startPosition">ドラッグ開始位置</param>
        private void StartGlobalDraggingInternal(Vector2 startPosition)
        {
            if (_isDragging_Internal) return;

            _isDragging_Internal = true;
            _draggingElement_Internal = null;
            _dragStartPosition_Internal = startPosition;
            _isDragging.Value = true;
            _dragDistance.Value = 0f;
            _globalDragStartedSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// 指定したフェーズに合致する *最初の* タッチを取得します。
        /// </summary>
        /// <param name="phase">検索するタッチフェーズ</param>
        /// <param name="touch">見つかったタッチ情報</param>
        /// <returns>合致するタッチが見つかった場合はtrue</returns>
        public bool TryGetTouchByPhase(TouchPhase phase, out Touch touch)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch currentTouch = Input.GetTouch(i);
                if (currentTouch.phase == phase)
                {
                    touch = currentTouch;
                    return true;
                }
            }
            touch = default;
            return false;
        }
    }
}
