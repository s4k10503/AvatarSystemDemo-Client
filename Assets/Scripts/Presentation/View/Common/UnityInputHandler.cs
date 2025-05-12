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
    /// タッチ入力とマウス入力を統一的に処理し、R3のObservableとして提供します。
    /// </summary>
    /// <remarks>
    /// Unityの入力ハンドラー
    /// </remarks>
    public sealed class UnityInputHandler : IInputHandler, IDisposable
    {
        #region Constants
        private const float SHORT_DRAG_THRESHOLD = 10f; // タップとみなす距離のしきい値(px)
        private const float MIN_PINCH_DISTANCE = 0.1f; // ピンチジェスチャーの最小距離
        private const float MOUSE_MOVE_THRESHOLD = 0.1f; // マウス移動の検出閾値
        #endregion

        #region Types
        private enum PointerEventType { None, Down, Up, Move }
        #endregion

        #region Fields
        private readonly UIDocument _uiDocument;
        private readonly CompositeDisposable _disposables;

        // ドラッグ関連
        private bool _isDragging_Internal;
        private Vector2 _dragStartPosition_Internal;
        private VisualElement _draggingElement_Internal;

        // ピンチ関連
        private float _lastPinchDistance;
        private bool _isPinching;
        #endregion

        #region R3 Subjects and Properties
        // ポインターイベント
        private readonly Subject<Vector2> _pointerDownSubject = new();
        private readonly Subject<Vector2> _pointerUpSubject = new();
        private readonly Subject<Vector2> _pointerMoveSubject = new();
        private readonly ReactiveProperty<Vector2> _currentPointerPosition = new(Vector2.zero);
        private readonly ReactiveProperty<Vector2> _previousPointerPosition = new(Vector2.zero);

        // ドラッグイベント
        private readonly ReactiveProperty<bool> _isDragging = new(false);
        private readonly ReactiveProperty<float> _dragDistance = new(0f);
        private readonly Subject<Unit> _globalDragStartedSubject = new();
        private readonly Subject<Unit> _globalDragEndedSubject = new();
        private readonly Subject<VisualElement> _elementDragStartedSubject = new();
        private readonly Subject<VisualElement> _elementDragEndedSubject = new();

        // ピンチイベント
        private readonly Subject<float> _pinchSubject = new();
        #endregion

        #region Public Properties
        public VisualElement DraggingElement => _draggingElement_Internal;
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
        public Observable<float> OnPinch => _pinchSubject;
        public bool IsShortDrag => _isDragging_Internal && _dragDistance.Value < SHORT_DRAG_THRESHOLD;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="uiDocument">UIドキュメント</param>
        public UnityInputHandler(UIDocument uiDocument)
        {
            _uiDocument = uiDocument;
            _disposables = new CompositeDisposable();
            Initialize();
        }

        /// <summary>
        /// 初期化処理を行います。
        /// </summary>
        private void Initialize()
        {
            InitializeFields();
            InitializeDisposables();
            StartUpdateLoop();
        }

        /// <summary>
        /// フィールドを初期化します。
        /// </summary>
        private void InitializeFields()
        {
            _isDragging_Internal = false;
            _dragStartPosition_Internal = Vector2.zero;
            _draggingElement_Internal = null;
            _lastPinchDistance = 0f;
            _isPinching = false;
        }

        /// <summary>
        /// 破棄可能オブジェクトを初期化します。
        /// </summary>
        private void InitializeDisposables()
        {
            var disposables = new IDisposable[] {
                // Subjects
                _pointerDownSubject,
                _pointerUpSubject,
                _pointerMoveSubject,
                _globalDragStartedSubject,
                _globalDragEndedSubject,
                _elementDragStartedSubject,
                _elementDragEndedSubject,
                _pinchSubject,

                // Reactive Properties
                _currentPointerPosition,
                _previousPointerPosition,
                _isDragging,
                _dragDistance
            };

            foreach (var disposable in disposables)
            {
                _disposables.Add(disposable);
            }
        }

        /// <summary>
        /// 更新ループを開始します。
        /// </summary>
        private void StartUpdateLoop()
        {
            Observable.EveryUpdate()
                .Subscribe(_ => UpdateInputState())
                .AddTo(_disposables);
        }

        /// <summary>
        /// リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            _disposables.Dispose();
        }

        #region Public Methods
        /// <summary>
        /// 現在のタッチ数を取得します。
        /// </summary>
        public int GetTouchCount() =>
            Input.touchCount;

        /// <summary>
        /// 指定したインデックスのタッチ情報を取得します。
        /// </summary>
        /// <param name="index">タッチのインデックス。</param>
        /// <returns>指定したインデックスのタッチ情報。</returns>
        public Touch? GetTouch(int index)
        {
            // インデックスが有効な場合はタッチ情報を取得
            return index >= 0 && index < Input.touchCount ? Input.GetTouch(index) : null;
        }

        /// <summary>
        /// 指定したフェーズに合致するすべてのタッチを取得します。
        /// </summary>
        /// <param name="phase">フェーズ。</param>
        /// <returns>指定したフェーズに合致するすべてのタッチ。</returns>
        public IEnumerable<Touch> GetTouchesByPhase(TouchPhase phase) =>
            Input.touches.Where(t => t.phase == phase);

        /// <summary>
        /// ポインターがUI上にあるかどうかを判定します。
        /// </summary>
        /// <param name="position">ポインターの位置。</param>
        /// <returns>ポインターがUI上にある場合はtrue、そうでない場合はfalse。</returns>
        public bool IsPointerOverUI(Vector2 position)
        {
            // UIドキュメントが存在しない場合はfalseを返す
            if (_uiDocument?.rootVisualElement == null) return false;

            // ポインターの位置をスクリーン座標に変換
            Vector2 screenPosition = new(position.x, Screen.height - position.y);

            // ポインターの位置がUI上にあるかどうかを判定
            return _uiDocument.rootVisualElement.panel.Pick(screenPosition) != null;
        }

        /// <summary>
        /// 要素のドラッグを開始します。
        /// </summary>
        public void StartDraggingOnElement(VisualElement element) =>
            StartDragging(element, _currentPointerPosition.Value);

        /// <summary>
        /// グローバルなドラッグを開始します。
        /// </summary>
        public void StartGlobalDragging() =>
            StartDragging(null, _currentPointerPosition.Value);

        /// <summary>
        /// ピンチジェスチャーを処理します。
        /// </summary>
        private void ProcessPinchGesture()
        {
            // タッチが2つでない場合はピンチジェスチャーを停止
            if (Input.touchCount != 2)
            {
                _isPinching = false;
                return;
            }

            var touch1 = Input.GetTouch(0);
            var touch2 = Input.GetTouch(1);

            // タッチが有効でない場合はピンチジェスチャーを停止
            if (!IsValidPinchTouch(touch1) || !IsValidPinchTouch(touch2))
            {
                _isPinching = false;
                return;
            }

            // ピンチジェスチャーの開始を判定
            if (IsPinchStart(touch1, touch2))
            {
                StartPinchGesture(touch1, touch2);
                return;
            }

            // ピンチジェスチャーの移動を判定
            if (_isPinching && IsPinchMoving(touch1, touch2))
            {
                ProcessPinchMovement(touch1, touch2);
            }
        }

        /// <summary>
        /// 有効なピンチタッチを判定します。
        /// </summary>
        /// <param name="touch">タッチ</param>
        /// <returns>有効な場合はtrue、無効な場合はfalse</returns>
        private bool IsValidPinchTouch(Touch touch) =>
            touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved;

        /// <summary>
        /// ピンチジェスチャーの開始を判定します。
        /// </summary>
        /// <param name="touch1">タッチ1</param>
        /// <param name="touch2">タッチ2</param>
        /// <returns>ピンチジェスチャーの開始の場合はtrue、そうでない場合はfalse</returns>
        private bool IsPinchStart(Touch touch1, Touch touch2) =>
            touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began || !_isPinching;

        /// <summary>
        /// ピンチジェスチャーの移動を判定します。
        /// </summary>
        /// <param name="touch1">タッチ1</param>
        /// <param name="touch2">タッチ2</param>
        /// <returns>ピンチジェスチャーの移動の場合はtrue、そうでない場合はfalse</returns>
        private bool IsPinchMoving(Touch touch1, Touch touch2) =>
            touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved;

        /// <summary>
        /// ピンチジェスチャーの開始を処理します。
        /// </summary>
        /// <param name="touch1">タッチ1</param>
        /// <param name="touch2">タッチ2</param>
        private void StartPinchGesture(Touch touch1, Touch touch2)
        {
            _lastPinchDistance = Vector2.Distance(touch1.position, touch2.position);
            _isPinching = true;
        }

        /// <summary>
        /// ピンチジェスチャーの移動を処理します。
        /// </summary>
        /// <param name="touch1">タッチ1</param>
        /// <param name="touch2">タッチ2</param>
        private void ProcessPinchMovement(Touch touch1, Touch touch2)
        {
            float currentPinchDistance = Vector2.Distance(touch1.position, touch2.position);
            if (_lastPinchDistance > MIN_PINCH_DISTANCE && currentPinchDistance > MIN_PINCH_DISTANCE)
            {
                _pinchSubject.OnNext(currentPinchDistance - _lastPinchDistance);
            }
            _lastPinchDistance = currentPinchDistance;
        }

        /// <summary>
        /// 最優先のポインターイベントを取得します。
        /// </summary>
        /// <param name="eventType">検出されたイベントのタイプ。</param>
        /// <param name="eventPosition">検出されたイベントの位置。</param>
        /// <returns>イベントが検出された場合はtrue、そうでない場合はfalse。</returns>
        private bool TryGetHighestPriorityPointerEvent(
            out PointerEventType eventType, out Vector2 eventPosition)
        {
            eventType = PointerEventType.None;
            eventPosition = Vector2.zero;

            if (Input.touchCount > 0)
            {
                return TryProcessTouchInput(out eventType, out eventPosition);
            }

            return TryProcessMouseInput(out eventType, out eventPosition);
        }

        /// <summary>
        /// タッチ入力を処理します。
        /// </summary>
        /// <param name="eventType">検出されたイベントのタイプ。</param>
        /// <param name="eventPosition">検出されたイベントの位置。</param>
        /// <returns>イベントが検出された場合はtrue、そうでない場合はfalse。</returns>
        private bool TryProcessTouchInput(
            out PointerEventType eventType, out Vector2 eventPosition)
        {
            ProcessPinchGesture();
            return TryGetTouchEvent(out eventType, out eventPosition);
        }

        /// <summary>
        /// タッチイベントを取得します。
        /// </summary>
        /// <param name="eventType">検出されたイベントのタイプ。</param>
        /// <param name="eventPosition">検出されたイベントの位置。</param>
        /// <returns>イベントが検出された場合はtrue、そうでない場合はfalse。</returns>
        private bool TryGetTouchEvent(out PointerEventType eventType, out Vector2 eventPosition)
        {
            var phases = new[] {
                (TouchPhase.Began, PointerEventType.Down),
                (TouchPhase.Ended, PointerEventType.Up),
                (TouchPhase.Moved, PointerEventType.Move)
            };

            foreach (var (phase, type) in phases)
            {
                if (TryGetTouchByPhase(phase, out var touch))
                {
                    eventType = type;
                    eventPosition = touch.position;
                    return true;
                }
            }

            eventType = PointerEventType.None;
            eventPosition = Vector2.zero;
            return false;
        }

        /// <summary>
        /// マウス入力を処理します。
        /// </summary>
        /// <param name="eventType">検出されたイベントのタイプ。</param>
        /// <param name="eventPosition">検出されたイベントの位置。</param>
        /// <returns>イベントが検出された場合はtrue、そうでない場合はfalse。</returns>
        private bool TryProcessMouseInput(
            out PointerEventType eventType, out Vector2 eventPosition)
        {
            eventType = PointerEventType.None;
            eventPosition = Vector2.zero;

            if (Input.GetMouseButtonDown(0))
            {
                eventType = PointerEventType.Down;
                eventPosition = Input.mousePosition;
                return true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                eventType = PointerEventType.Up;
                eventPosition = Input.mousePosition;
                return true;
            }

            if (Input.GetMouseButton(0))
            {
                Vector2 currentMousePos = Input.mousePosition;
                if (Vector2.Distance(currentMousePos, _previousPointerPosition.Value) > MOUSE_MOVE_THRESHOLD)
                {
                    eventType = PointerEventType.Move;
                    eventPosition = currentMousePos;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定されたフェーズに一致するタッチを取得します。
        /// </summary>
        /// <param name="phase">検索するタッチフェーズ</param>
        /// <param name="touch">見つかったタッチ</param>
        /// <returns>見つかった場合はtrue、見つからない場合はfalse</returns>
        public bool TryGetTouchByPhase(TouchPhase phase, out Touch touch)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var currentTouch = Input.GetTouch(i);
                if (currentTouch.phase == phase)
                {
                    touch = currentTouch;
                    return true;
                }
            }
            touch = default;
            return false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 入力状態を更新します。
        /// </summary>
        private void UpdateInputState()
        {
            UpdatePointerPositions();

            if (TryGetHighestPriorityPointerEvent(out var eventType, out var eventPosition))
            {
                ProcessPointerEvent(eventType, eventPosition);
            }
        }

        /// <summary>
        /// ポインターイベントを処理します。
        /// </summary>
        /// <param name="eventType">イベントのタイプ。</param>
        /// <param name="position">イベントの位置。</param>
        private void ProcessPointerEvent(PointerEventType eventType, Vector2 position)
        {
            switch (eventType)
            {
                case PointerEventType.Down:
                    ProcessPointerDown(position);
                    break;
                case PointerEventType.Up:
                    ProcessPointerUp(position);
                    break;
                case PointerEventType.Move:
                    ProcessPointerMove(position);
                    break;
            }
        }

        /// <summary>
        /// ポインター位置を更新します。
        /// </summary>
        private void UpdatePointerPositions()
        {
            _previousPointerPosition.Value = _currentPointerPosition.Value;
            _currentPointerPosition.Value = GetCurrentRawPointerPosition();

            // ドラッグ中の場合、ドラッグ距離を更新
            if (_isDragging_Internal)
            {
                _dragDistance.Value = Vector2.Distance(_currentPointerPosition.Value, _dragStartPosition_Internal);
            }
        }

        /// <summary>
        /// 現在のポインター位置を取得します。
        /// </summary>
        /// <returns>現在のポインター位置。</returns>
        private Vector2 GetCurrentRawPointerPosition() =>
            Input.touchCount > 0 ? Input.GetTouch(0).position : Input.mousePosition;

        /// <summary>
        /// ポインターの下がりを処理します。
        /// </summary>
        /// <param name="downPosition">ポインターの下がり位置。</param>
        private void ProcessPointerDown(Vector2 downPosition)
        {
            _pointerDownSubject.OnNext(downPosition);
            if (!IsPointerOverUI(downPosition) && _draggingElement_Internal == null)
            {
                StartDragging(null, downPosition);
            }
        }

        /// <summary>
        /// ポインターの上がりを処理します。
        /// </summary>
        /// <param name="upPosition">上がり位置。</param>
        private void ProcessPointerUp(Vector2 upPosition)
        {
            _pointerUpSubject.OnNext(upPosition);
            if (_isDragging_Internal)
            {
                StopDragging(_draggingElement_Internal);
            }
        }

        /// <summary>
        /// ポインターの移動を処理します。
        /// </summary>
        /// <param name="movePosition">移動位置。</param>
        private void ProcessPointerMove(Vector2 movePosition)
        {
            _pointerMoveSubject.OnNext(movePosition);
        }

        /// <summary>
        /// ドラッグを開始します。
        /// </summary>
        /// <param name="element">ドラッグを開始する要素。nullの場合はグローバルドラッグ。</param>
        /// <param name="startPosition">ドラッグの開始位置。</param>
        private void StartDragging(VisualElement element, Vector2 startPosition)
        {
            if (_isDragging_Internal) return;

            _isDragging_Internal = true;
            _draggingElement_Internal = element;
            _dragStartPosition_Internal = startPosition;
            _isDragging.Value = true;
            _dragDistance.Value = 0f;

            if (element != null)
            {
                _elementDragStartedSubject.OnNext(element);
            }
            else
            {
                _globalDragStartedSubject.OnNext(Unit.Default);
            }
        }

        /// <summary>
        /// ドラッグを停止します。
        /// </summary>
        /// <param name="element">ドラッグを停止する要素。nullの場合はグローバルドラッグ。</param>
        private void StopDragging(VisualElement element)
        {
            if (!_isDragging_Internal || _draggingElement_Internal != element) return;

            _isDragging_Internal = false;
            _draggingElement_Internal = null;
            _isDragging.Value = false;

            if (element != null)
            {
                _elementDragEndedSubject.OnNext(element);
            }
            else
            {
                _globalDragEndedSubject.OnNext(Unit.Default);
            }
        }
        #endregion
    }
}
