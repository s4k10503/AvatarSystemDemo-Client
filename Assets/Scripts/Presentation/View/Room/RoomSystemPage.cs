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
        private Button _jumpButton;
        private Button _logoutButton;
        private Button _navigateToAvatarSystemButton;

        // ロジックコンポーネント
        private SlidePadComponent _slidePadLogic;

        // Subject/Observable
        private ReactiveProperty<Vector2> _slidePadDirectionProperty;
        public ReadOnlyReactiveProperty<Vector2> SlidePadDirection
            => _slidePadDirectionProperty ??= new ReactiveProperty<Vector2>(Vector2.zero);

        private Subject<Unit> _jumpButtonClickedSubject;
        public Observable<Unit> OnJumpButtonClicked
            => _jumpButtonClickedSubject ??= new Subject<Unit>();

        private Subject<Unit> _logoutRequestedSubject;
        public Observable<Unit> OnLogoutRequested => _logoutRequestedSubject ??= new Subject<Unit>();

        private Subject<Unit> _navigateToAvatarSystemRequestedSubject;
        public Observable<Unit> OnNavigateToAvatarSystemRequested
            => _navigateToAvatarSystemRequestedSubject ??= new Subject<Unit>();

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
            Initialize();
            Debug.Log("[RoomSystemPage] 有効になりました。");
        }

        /// <summary>
        /// 初期化処理を行います。
        /// </summary>
        private void Initialize()
        {
            InitializeFields();
            InitializeUIElements();
            SetupListeners();
            InitializeDisposables();
            SubscribeToObservables();
        }

        /// <summary>
        /// フィールドの初期化
        /// </summary>
        private void InitializeFields()
        {
            _disposables = new CompositeDisposable();
            _slidePadLogic = new SlidePadComponent();
            _slidePadDirectionProperty = (ReactiveProperty<Vector2>)SlidePadDirection;
            _jumpButtonClickedSubject = (Subject<Unit>)OnJumpButtonClicked;
            _logoutRequestedSubject = (Subject<Unit>)OnLogoutRequested;
            _navigateToAvatarSystemRequestedSubject = (Subject<Unit>)OnNavigateToAvatarSystemRequested;

            // Input Handlerの初期化
            if (TryGetDocument())
            {
                try
                {
                    _inputHandler = new UnityInputHandler(document);
                    Debug.Log("[RoomSystemPage] InputHandler が初期化されました。");
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
            _initialized = false; // Mimicking AvatarSystemPage
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
        private void InitializeUIElements()
        {
            if (!TryGetDocument() || _inputHandler == null) // InputHandlerも必須
            {
                Debug.LogError("[RoomSystemPage] UI要素の初期化に必要なコンポーネントが不足しています。", this);
                _initialized = true; // Set true even if failing to prevent issues if part of UI is up
                return;
            }

            InitializeSlidePadElements();
            InitializeJumpButton();
            InitializeAdditionalButtons();

            // TODO: RoomSystemPageの他のUI要素をここで初期化
            _initialized = true; // Mimicking AvatarSystemPage
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
                Debug.Log("[RoomSystemPage] SlidePad要素がロジックに設定されました。");
            }
            else
            {
                string errorReason = _slidePadLogic == null ? "SlidePadLogicがnullです" : "SlidePad要素が見つかりません";
                Debug.LogError($"[RoomSystemPage] SlidePad要素をロジックに設定できません。理由: {errorReason}", this);
            }
        }

        /// <summary>
        /// ジャンプボタンの初期化
        /// </summary>
        private void InitializeJumpButton()
        {
            if (document == null || document.rootVisualElement == null) return;

            var root = document.rootVisualElement;
            _jumpButton = UIElementUtils.QueryAndCheck<Button>(root, elementName: "jump-button", context: this);

            if (_jumpButton != null)
            {
                Debug.Log("[RoomSystemPage] ジャンプボタンが初期化されました。");
            }
        }

        /// <summary>
        /// 追加ボタン（ログアウト、アバター設定へ）の初期化
        /// </summary>
        private void InitializeAdditionalButtons()
        {
            if (document == null || document.rootVisualElement == null) return;
            var root = document.rootVisualElement;

            _logoutButton = UIElementUtils.QueryAndCheck<Button>(root, elementName: "logout-button", context: this);
            _navigateToAvatarSystemButton = UIElementUtils.QueryAndCheck<Button>(root, elementName: "navigate-to-avatar-system-button", context: this);

            if (_logoutButton != null)
            {
                Debug.Log("[RoomSystemPage] ログアウトボタンが初期化されました。");
            }
            if (_navigateToAvatarSystemButton != null)
            {
                Debug.Log("[RoomSystemPage] アバター設定へ移動ボタンが初期化されました。");
            }
        }

        /// <summary>
        /// 全てのリスナーの設定
        /// </summary>
        private void SetupListeners()
        {
            // 要素とロジックが有効な場合にスライドパッドリスナーを設定
            if (_slidePadBackgroundElement != null && _slidePadHandleElement != null && _slidePadLogic != null)
            {
                SetupSlidePadListeners();
            }
            else
            {
                Debug.Log("[RoomSystemPage] 要素/ロジックが見つからないため、SlidePadリスナーのセットアップをスキップします。");
            }

            // ボタンリスナーの設定
            SetupButtonListeners();

            // TODO: RoomSystemPageの他のコントロールのリスナーをここで設定
        }

        /// <summary>
        /// スライドパッドのリスナー設定
        /// </summary>
        private void SetupSlidePadListeners()
        {
            // ガード節（SetupListenersでのチェックにより多少冗長）
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
            Observable.EveryUpdate()
                .Select(_ => _slidePadLogic.Direction)
                .DistinctUntilChanged()
                .Subscribe(direction => _slidePadDirectionProperty.Value = direction)
                .AddTo(_disposables);

            Debug.Log("[RoomSystemPage] SlidePadリスナーの設定が完了しました。");
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
                Debug.Log("[RoomSystemPage] スライドパッドのドラッグリクエストは無視されました: 他のドラッグが進行中です。");
                return;
            }

            // InputHandler経由でドラッグを開始し、背景要素に関連付ける
            _inputHandler.StartDraggingOnElement(backgroundElement);

            // InputHandler経由でドラッグを開始した後にポインターをキャプチャ
            // TODO: マルチタッチを直接サポートする場合、正しいpointerIdを取得する方法が必要
            if (!backgroundElement.HasPointerCapture(PointerId.mousePointerId))
            {
                backgroundElement.CapturePointer(PointerId.mousePointerId); // 現時点ではマウス/タッチ0を想定
                Debug.Log("[RoomSystemPage] スライドパッドのポインターがキャプチャされました。");
            }
            else
            {
                Debug.Log("[RoomSystemPage] スライドパッドのポインターは既にキャプチャされています。");
            }
            Debug.Log("[RoomSystemPage] スライドパッドのドラッグが開始されました (InputHandler経由)。");
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

            // スライドパッドが実際にドラッグされていたかを確認
            // ドラッグ終了はポインターアップ時にInputHandler内部で処理される
            // SlidePadComponentの内部状態がリセットされることだけ確認する
            if (_inputHandler.DraggingElement == _slidePadBackgroundElement)
            {
                // InputHandler.StopDragging(); // Obsolete and handled internally
                Debug.Log("[RoomSystemPage] スライドパッドのドラッグが終了しました(内部処理)。");
            }
            else
            {
                // ドラッグが終了したがスライドパッドではなかった場合
                if (_inputHandler.IsDragging.CurrentValue)
                {
                    Debug.Log("[RoomSystemPage] DragEndRequest受信時にスライドパッド以外の要素がドラッグ中でした。");
                }
            }
        }

        /// <summary>
        /// ボタンのリスナー設定
        /// </summary>
        private void SetupButtonListeners()
        {
            if (_jumpButton != null)
            {
                _jumpButton.clicked += () => _jumpButtonClickedSubject?.OnNext(Unit.Default);
                Debug.Log("[RoomSystemPage] ジャンプボタンのリスナーが設定されました。");
            }
            else
            {
                Debug.LogWarning("[RoomSystemPage] ジャンプボタンが見つからないため、リスナーを設定できません。", this);
            }

            if (_logoutButton != null)
            {
                _logoutButton.clicked += () => _logoutRequestedSubject?.OnNext(Unit.Default);
                Debug.Log("[RoomSystemPage] ログアウトボタンのリスナーが設定されました。");
            }
            else
            {
                Debug.LogWarning("[RoomSystemPage] ログアウトボタンが見つからないため、リスナーを設定できません。", this);
            }

            if (_navigateToAvatarSystemButton != null)
            {
                _navigateToAvatarSystemButton.clicked += () => _navigateToAvatarSystemRequestedSubject?.OnNext(Unit.Default);
                Debug.Log("[RoomSystemPage] アバター設定へ移動ボタンのリスナーが設定されました。");
            }
            else
            {
                Debug.LogWarning("[RoomSystemPage] アバター設定へ移動ボタンが見つからないため、リスナーを設定できません。", this);
            }
        }

        /// <summary>
        /// 破棄可能オブジェクトを初期化します。
        /// </summary>
        private void InitializeDisposables()
        {
            _disposables ??= new CompositeDisposable();
            _disposables.Add(_slidePadDirectionProperty);
            _disposables.Add(_jumpButtonClickedSubject);
            _disposables.Add(_logoutRequestedSubject);
            _disposables.Add(_navigateToAvatarSystemRequestedSubject);
        }

        /// <summary>
        /// 無効になった時の処理
        /// </summary>
        private void OnDisable()
        {
            Debug.Log($"[RoomSystemPage] 無効化中。初期化済み: {_initialized}");
            CleanupResources();
            _initialized = false;
            Debug.Log("[RoomSystemPage] 無効になりました。");
        }

        /// <summary>
        /// スライドパッドのコールバック登録解除
        /// </summary>
        private void UnregisterSlidePadCallbacks()
        {
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
                    Debug.LogWarning($"[RoomSystemPage] スライドパッドコールバック登録解除中にエラーが発生しました: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// UI要素の参照をクリア
        /// </summary>
        private void ClearUIElementReferences()
        {
            _slidePadBackgroundElement = null;
            _slidePadHandleElement = null;
            _jumpButton = null;
            _logoutButton = null;
            _navigateToAvatarSystemButton = null;
        }

        /// <summary>
        /// リソースのクリーンアップを行う共通メソッド
        /// </summary>
        private void CleanupResources()
        {
            _disposables?.Dispose();
            _disposables = null;

            if (_slidePadLogic != null)
            {
                _slidePadLogic.OnRequestDragStart -= HandleSlidePadDragStartRequest;
                _slidePadLogic.OnRequestDragEnd -= HandleSlidePadDragEndRequest;
            }
            UnregisterSlidePadCallbacks();
            ClearUIElementReferences();
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        private void OnDestroy()
        {
            CleanupResources();
            _slidePadLogic = null;
            _inputHandler = null;
        }

        /// <summary>
        /// Observableへの登録とイベント発行を行うメソッド群
        /// </summary>
        private void SubscribeToObservables()
        {
            if (!_initialized || _slidePadLogic == null || _inputHandler == null || _slidePadDirectionProperty == null)
            {
                Debug.LogWarning("[RoomSystemPage] SubscribeToObservablesの前提条件が満たされていません。処理をスキップします。", this);
                return;
            }

            Debug.Log("[RoomSystemPage] Observableの購読を開始しました。(SubscribeToObservables)");
        }
    }
}
