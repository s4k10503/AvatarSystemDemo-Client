using UnityEngine;
using UnityEngine.UIElements;
using R3;
using System;
using Presentation.Utils;

using Presentation.Interfaces;

namespace Presentation.View
{
    /// <summary>
    /// ルームシステムページ
    /// </summary>
    public sealed class RoomSystemPage : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        // UI要素
        private VisualElement _slidePadBackgroundElement;
        private VisualElement _slidePadHandleElement;

        // ロジックコンポーネント
        private SlidePadComponent _slidePadLogic;

        // Subject/Observable
        private Subject<Vector2> _slidePadDirectionSubject;
        public Observable<Vector2> OnSlidePadDirectionChanged => _slidePadDirectionSubject ??= new Subject<Vector2>();

        private Subject<string> _logMessageSubject;
        public Observable<string> OnLogMessage => _logMessageSubject ??= new Subject<string>();

        // 依存関係
        private IInputHandler _inputHandler;

        // Disposable
        private CompositeDisposable _disposables;

        // 状態
        private bool _initialized = false;

        /// <summary>
        /// 有効になった時の処理
        /// </summary>
        private void OnEnable()
        {
            // コアコンポーネントの初期化
            _disposables = new CompositeDisposable();
            _slidePadLogic = new SlidePadComponent();
            _logMessageSubject = new Subject<string>();
            _slidePadDirectionSubject = new Subject<Vector2>();

            // Input Handlerの初期化
            if (TryGetDocument())
            {
                try
                {
                    _inputHandler = new UnityInputHandler(document);
                    _logMessageSubject?.OnNext("[RoomSystemPage] InputHandler が初期化されました。");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RoomSystemPage] UnityInputHandler の初期化に失敗しました: {ex.Message}", this);
                    _inputHandler = null;
                }
            }
            else
            {
                Debug.LogError("[RoomSystemPage] UIDocument が見つかりません。InputHandler を初期化できません。", this);
                _inputHandler = null;
            }

            // InputHandlerが正常に作成された場合のみ続行
            if (_inputHandler == null)
            {
                Debug.LogError("[RoomSystemPage] InputHandler が見つからないため、初期化を中止しました。", this);
                return;
            }

            // UIとリスナーの初期化
            InitializeAllUIElements(); // 最初にUI要素を検索
            SetupAllListeners();       // 次にリスナーを設定

            _initialized = true;
            _logMessageSubject?.OnNext("[RoomSystemPage] 有効になりました。");
        }

        /// <summary>
        /// UIDocumentを取得する
        /// </summary>
        private bool TryGetDocument()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
            return document != null && document.rootVisualElement != null;
        }

        /// <summary>
        /// 全てのUI要素の初期化
        /// </summary>
        private void InitializeAllUIElements()
        {
            if (!TryGetDocument()) return;

            InitializeSlidePadElements();

            // TODO: RoomSystemPageの他のUI要素をここで初期化
        }

        /// <summary>
        /// スライドパッド関連UI要素の初期化
        /// </summary>
        private void InitializeSlidePadElements()
        {
            if (document == null || document.rootVisualElement == null) return;

            var root = document.rootVisualElement;
            var slidePadInstanceContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "slide-pad-instance", context: this);

            // slidePadInstanceContainer が見つかった場合のみ子要素を検索
            if (slidePadInstanceContainer != null)
            {
                _slidePadBackgroundElement = UIElementUtils.QueryAndCheck<VisualElement>(slidePadInstanceContainer, elementName: "slide-pad-background", context: this);
                _slidePadHandleElement = UIElementUtils.QueryAndCheck<VisualElement>(slidePadInstanceContainer, elementName: "slide-pad-handle", context: this);
            }
            // QueryAndCheck内でエラーログは出力されるため、ここでの個別ログは不要

            // ロジックコンポーネントに要素を割り当て
            if (_slidePadLogic != null && _slidePadBackgroundElement != null && _slidePadHandleElement != null)
            {
                _slidePadLogic.SetElements(_slidePadBackgroundElement, _slidePadHandleElement);
                _logMessageSubject?.OnNext("[RoomSystemPage] SlidePad要素がロジックに設定されました。");
            }
            else
            {
                string errorReason = _slidePadLogic == null ? "SlidePadLogicがnullです" : "SlidePad要素が見つかりません";
                Debug.LogError($"[RoomSystemPage] SlidePad要素をロジックに設定できません。理由: {errorReason}", this);
            }
        }


        /// <summary>
        /// 全てのリスナーの設定
        /// </summary>
        private void SetupAllListeners()
        {
            // 要素とロジックが有効な場合にスライドパッドリスナーを設定
            if (_slidePadBackgroundElement != null && _slidePadHandleElement != null && _slidePadLogic != null)
            {
                SetupSlidePadListeners();
            }
            else
            {
                _logMessageSubject?.OnNext("[RoomSystemPage] 要素/ロジックが見つからないため、SlidePadリスナーのセットアップをスキップします。");
            }

            // TODO: RoomSystemPageの他のコントロールのリスナーをここで設定
        }

        /// <summary>
        /// スライドパッドのリスナー設定
        /// </summary>
        private void SetupSlidePadListeners()
        {
            // ガード節（SetupAllListenersでのチェックにより多少冗長）
            if (_slidePadBackgroundElement == null)
            {
                Debug.LogError("[RoomSystemPage] スライドパッドリスナーを設定できません: 背景要素がnullです。", this);
                return;
            }
            if (_slidePadLogic == null)
            {
                Debug.LogError("[RoomSystemPage] スライドパッドリスナーを設定できません: ロジックハンドラがnullです。", this);
                return;
            }

            // SlidePadComponentイベントの購読
            // OnEnableが複数回呼び出された場合の重複を防ぐために、まず購読解除
            _slidePadLogic.OnRequestDragStart -= HandleSlidePadDragStartRequest;
            _slidePadLogic.OnRequestDragEnd -= HandleSlidePadDragEndRequest;
            // 購読
            _slidePadLogic.OnRequestDragStart += HandleSlidePadDragStartRequest;
            _slidePadLogic.OnRequestDragEnd += HandleSlidePadDragEndRequest;


            // 背景要素へのコールバック登録
            // 既存のコールバックを最初にクリア
            _slidePadBackgroundElement.UnregisterCallback<PointerDownEvent>(_slidePadLogic.OnPointerDown);
            _slidePadBackgroundElement.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
            _slidePadBackgroundElement.UnregisterCallback<PointerUpEvent>(_slidePadLogic.OnPointerUp);
            _slidePadBackgroundElement.UnregisterCallback<PointerCaptureOutEvent>(_slidePadLogic.OnPointerCaptureOut);
            // 新しいコールバックを登録
            _slidePadBackgroundElement.RegisterCallback<PointerDownEvent>(_slidePadLogic.OnPointerDown);
            _slidePadBackgroundElement.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            _slidePadBackgroundElement.RegisterCallback<PointerUpEvent>(_slidePadLogic.OnPointerUp);
            _slidePadBackgroundElement.RegisterCallback<PointerCaptureOutEvent>(_slidePadLogic.OnPointerCaptureOut);


            // スライドパッド方向変更の購読
            // 購読前にSubjectが初期化されていることを確認（OnEnableで実行済み）
            Observable.EveryUpdate()
                .Select(_ => _slidePadLogic.Direction)
                .DistinctUntilChanged()
                .Subscribe(direction => _slidePadDirectionSubject?.OnNext(direction))
                .AddTo(_disposables);

            _logMessageSubject?.OnNext("[RoomSystemPage] SlidePadリスナーの設定が完了しました。");
        }

        /// <summary>
        /// PointerMoveEventのハンドラー
        /// </summary>
        private void HandlePointerMove(PointerMoveEvent evt)
        {
            // SlidePadLogicで移動を処理する前にInputHandlerの状態を確認
            if (_inputHandler != null
                && _inputHandler.IsDragging.CurrentValue
                && _inputHandler.DraggingElement == _slidePadBackgroundElement
                && _slidePadBackgroundElement.HasPointerCapture(evt.pointerId))
            {
                _slidePadLogic?.OnPointerMove(evt);
            }
        }

        /// <summary>
        /// スライドパッドのドラッグ開始リクエストのハンドラー
        /// </summary>
        private void HandleSlidePadDragStartRequest(VisualElement backgroundElement)
        {
            if (_inputHandler == null)
            {
                Debug.LogWarning("[RoomSystemPage] DragStartRequestでInputHandlerがnullです。");
                return;
            }
            if (backgroundElement == null)
            {
                Debug.LogWarning("[RoomSystemPage] DragStartRequestでBackgroundElementがnullです。");
                return;
            }

            // 他のドラッグがグローバルに進行中かどうかを確認
            if (_inputHandler.IsDragging.CurrentValue)
            {
                _logMessageSubject?.OnNext("[RoomSystemPage] スライドパッドのドラッグリクエストは無視されました: 他のドラッグが進行中です。");
                return;
            }

            // InputHandler経由でドラッグを開始し、背景要素に関連付ける
            _inputHandler.StartDraggingOnElement(backgroundElement);

            // InputHandler経由でドラッグを開始した後にポインターをキャプチャ
            // TODO: マルチタッチを直接サポートする場合、正しいpointerIdを取得する方法が必要
            if (!backgroundElement.HasPointerCapture(PointerId.mousePointerId))
            {
                backgroundElement.CapturePointer(PointerId.mousePointerId); // 現時点ではマウス/タッチ0を想定
                _logMessageSubject?.OnNext("[RoomSystemPage] スライドパッドのポインターがキャプチャされました。");
            }
            else
            {
                _logMessageSubject?.OnNext("[RoomSystemPage] スライドパッドのポインターは既にキャプチャされています。");
            }
            _logMessageSubject?.OnNext("[RoomSystemPage] スライドパッドのドラッグが開始されました (InputHandler経由)。");
        }

        /// <summary>
        /// スライドパッドのドラッグ終了リクエストのハンドラー
        /// </summary>
        private void HandleSlidePadDragEndRequest()
        {
            if (_inputHandler == null)
            {
                Debug.LogWarning("[RoomSystemPage] DragEndRequestでInputHandlerがnullです。");
                return;
            }

            // Check if we were actually dragging the slide pad
            // Drag end is handled internally by InputHandler on pointer up.
            // We might not need to explicitly call StopDragging or similar here anymore.
            // Just ensure the SlidePadComponent resets its internal state.
            if (_inputHandler.DraggingElement == _slidePadBackgroundElement)
            {
                // InputHandler.StopDragging(); // Obsolete and handled internally
                _logMessageSubject?.OnNext("[RoomSystemPage] スライドパッドのドラッグが終了しました(内部処理)。");
            }
            else
            {
                // Log if drag ended but wasn't the slide pad (might indicate an issue)
                if (_inputHandler.IsDragging.CurrentValue) // Use .CurrentValue
                {
                    _logMessageSubject?.OnNext("[RoomSystemPage] DragEndRequest受信時にスライドパッド以外の要素がドラッグ中でした。");
                }
            }
        }

        /// <summary>
        /// 無効になった時の処理
        /// </summary>
        private void OnDisable()
        {
            _logMessageSubject?.OnNext($"[RoomSystemPage] 無効化中。初期化済み: {_initialized}");

            // 購読を破棄
            _disposables?.Dispose();
            _disposables = null;

            // ロジックが存在する場合、SlidePadComponentイベントから手動で購読解除
            if (_slidePadLogic != null)
            {
                _slidePadLogic.OnRequestDragStart -= HandleSlidePadDragStartRequest;
                _slidePadLogic.OnRequestDragEnd -= HandleSlidePadDragEndRequest;
            }

            // UI要素に手動で追加されたコールバックを解除
            if (_slidePadBackgroundElement != null && _slidePadLogic != null)
            {
                try
                {
                    _slidePadBackgroundElement.UnregisterCallback<PointerDownEvent>(_slidePadLogic.OnPointerDown);
                    _slidePadBackgroundElement.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
                    _slidePadBackgroundElement.UnregisterCallback<PointerUpEvent>(_slidePadLogic.OnPointerUp);
                    _slidePadBackgroundElement.UnregisterCallback<PointerCaptureOutEvent>(_slidePadLogic.OnPointerCaptureOut);
                }
                catch (Exception ex)
                {
                    // 要素/ロジックが無効になった場合の登録解除中の潜在的なエラーをキャッチ
                    Debug.LogWarning($"[RoomSystemPage] コールバック登録解除中にエラーが発生しました: {ex.Message}");
                }
            }

            _initialized = false;
            _slidePadBackgroundElement = null;
            _slidePadHandleElement = null;

            _logMessageSubject?.OnNext("[RoomSystemPage] 無効になりました。");
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        private void OnDestroy()
        {
            _disposables?.Dispose();

            _logMessageSubject?.Dispose();
            _slidePadDirectionSubject?.Dispose();

            _logMessageSubject = null;
            _slidePadDirectionSubject = null;
        }
    }
}