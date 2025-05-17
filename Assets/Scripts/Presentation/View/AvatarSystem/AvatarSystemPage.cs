using UnityEngine;
using UnityEngine.UIElements;
using R3;
using Domain.ValueObjects;
using System;
using Presentation.Utils;
using System.Collections.Generic;

using Presentation.Interfaces;
using System.Linq;

namespace Presentation.View
{
    /// <summary>
    /// アバターシステムページ
    /// </summary>
    /// <remarks>
    /// アバターシステムページ。
    /// </remarks>
    public sealed class AvatarSystemPage : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        // UI Elements
        private Slider _heightSlider;
        private Slider _shoulderWidthSlider;
        private Slider _bodyWidthSlider;
        private Slider _headSizeSlider;
        private Button _resetButton;
        private Button _saveButton;
        private Button _logoutButton;
        private Button _navigateToRoomButton;
        private VisualElement _contentContainer;
        private VisualElement _tabBarContainer;
        private VisualElement _tabBarScrollContent;
        private Button _bodyTab;
        private Button _faceTab;
        private VisualElement _mainContainer;
        private VisualElement _buttonContainer;

        // 肌色選択用のUI要素
        private VisualElement _skinColorContainer;
        private Button[] _skinColorPresetButtons;

        // 髪色選択用のUI要素
        private VisualElement _hairColorContainer;
        private Button[] _hairColorPresetButtons;

        // Event Handlers
        private readonly PageContentHandler _contentHandler = new();
        private readonly TabHandler _tabHandler = new();

        private bool _initialized = false;
        private bool _isUIVisible = true;
        private float _lastUIToggleTime = 0f;
        private const float UI_TOGGLE_COOLDOWN = 0.5f;
        private bool _isSwitchingTabs = false; // Flag to prevent recursive tab switching
        private int _currentPageIndex = 0; // Track the current page index

        // Reactive Properties for data binding
        private ReactiveProperty<float> _heightProperty;
        private ReactiveProperty<float> _shoulderWidthProperty;
        private ReactiveProperty<float> _bodyWidthProperty;
        private ReactiveProperty<float> _headSizeProperty;
        private ReactiveProperty<SkinColor> _skinColorProperty;

        // 髪色用 Reactive Property
        private ReactiveProperty<HairColor> _hairColorProperty;

        // R3 Subjects
        private Subject<Unit> _resetRequestedSubject;
        private Subject<Unit> _saveRequestedSubject;
        private Subject<Unit> _logoutRequestedSubject;
        private Subject<Unit> _navigateToRoomRequestedSubject;
        private Subject<Vector2> _nonUIPointerUpSubject;
        private Subject<bool> _uiVisibilityChangedSubject;
        private Subject<int> _tabChangedSubject;
        private Subject<Vector2> _cameraRotationDeltaSubject;
        private Subject<float> _cameraZoomRequestedSubject;
        private Subject<float> _cameraHeightDeltaSubject;

        // 肌色プリセット
        private SkinColor[] _skinColorPresets;

        // 髪色プリセット
        private HairColor[] _hairColorPresets;


        // Observable properties
        public ReadOnlyReactiveProperty<float> Height
            => _heightProperty ??= new ReactiveProperty<float>();
        public ReadOnlyReactiveProperty<float> ShoulderWidth
            => _shoulderWidthProperty ??= new ReactiveProperty<float>();
        public ReadOnlyReactiveProperty<float> BodyWidth
            => _bodyWidthProperty ??= new ReactiveProperty<float>();
        public ReadOnlyReactiveProperty<float> HeadSize
            => _headSizeProperty ??= new ReactiveProperty<float>();
        public ReadOnlyReactiveProperty<SkinColor> SkinColor
            => _skinColorProperty ??= new ReactiveProperty<SkinColor>(Domain.ValueObjects.SkinColor.Default);
        public ReadOnlyReactiveProperty<HairColor> HairColor
            => _hairColorProperty ??= new ReactiveProperty<HairColor>(Domain.ValueObjects.HairColor.Default);

        public Observable<Unit> OnResetRequested
            => _resetRequestedSubject ??= new Subject<Unit>();
        public Observable<Unit> OnSaveRequested
            => _saveRequestedSubject ??= new Subject<Unit>();
        public Observable<Unit> OnLogoutRequested
            => _logoutRequestedSubject ??= new Subject<Unit>();
        public Observable<Unit> OnNavigateToRoomRequested
            => _navigateToRoomRequestedSubject ??= new Subject<Unit>();
        public Observable<Vector2> OnNonUIPointerUp
            => _nonUIPointerUpSubject ??= new Subject<Vector2>();
        public Observable<bool> OnUIVisibilityChanged
            => _uiVisibilityChangedSubject ??= new Subject<bool>();
        public Observable<int> OnTabChanged
            => _tabChangedSubject ??= new Subject<int>();
        public Observable<Vector2> OnCameraRotateRequested
            => _cameraRotationDeltaSubject ??= new Subject<Vector2>();
        public Observable<float> OnCameraZoomRequested
            => _cameraZoomRequestedSubject ??= new Subject<float>();
        public Observable<float> OnCameraHeightRequested
            => _cameraHeightDeltaSubject ??= new Subject<float>();

        private CompositeDisposable _disposables;

        private const int NumberOfTabs = 2;

        private IInputHandler _inputHandler;

        /// <summary>
        /// 有効になった時の処理
        /// </summary>
        private void OnEnable()
        {
            Initialize();
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
            SubscribeToInputHandlerEvents();
        }

        /// <summary>
        /// フィールドを初期化します。
        /// </summary>
        private void InitializeFields()
        {
            // 肌色プリセットを初期化
            _skinColorPresets = Domain.ValueObjects.SkinColor.GetPresets();
            // 髪色プリセットを初期化
            _hairColorPresets = Domain.ValueObjects.HairColor.GetPresets();

            _contentHandler.Reset();
            _tabHandler.Reset();

            // ReactivePropertyを初期化
            _heightProperty = new ReactiveProperty<float>();
            _shoulderWidthProperty = new ReactiveProperty<float>();
            _bodyWidthProperty = new ReactiveProperty<float>();
            _headSizeProperty = new ReactiveProperty<float>();
            _skinColorProperty = new ReactiveProperty<SkinColor>(Domain.ValueObjects.SkinColor.Default);
            _hairColorProperty = new ReactiveProperty<HairColor>(Domain.ValueObjects.HairColor.Default);

            // Subjectを初期化
            _resetRequestedSubject = new Subject<Unit>();
            _saveRequestedSubject = new Subject<Unit>();
            _logoutRequestedSubject = new Subject<Unit>();
            _navigateToRoomRequestedSubject = new Subject<Unit>();
            _nonUIPointerUpSubject = new Subject<Vector2>();
            _uiVisibilityChangedSubject = new Subject<bool>();
            _tabChangedSubject = new Subject<int>();
            _cameraRotationDeltaSubject = new Subject<Vector2>();
            _cameraZoomRequestedSubject = new Subject<float>();
            _cameraHeightDeltaSubject = new Subject<float>();

            // UIDocumentが必要なため、ここでInputHandlerインスタンスを作成
            if (TryGetDocument())
            {
                _inputHandler = new UnityInputHandler(document);
                // InputHandlerを依存コンポーネントにも設定
                _tabHandler.InputHandler = _inputHandler;
                _contentHandler.InputHandler = _inputHandler;
            }
            else
            {
                // エラーログはTryGetDocument内で出力される
                return; // InputHandlerが作成できない場合は初期化を中断
            }

            // UIの表示状態を初期化（常に表示状態で開始）
            _isUIVisible = true;
            _lastUIToggleTime = Time.time;
            _isSwitchingTabs = false;
            _currentPageIndex = 0;
            _initialized = false;
        }

        /// <summary>
        /// UI要素の初期化
        /// </summary>
        private void InitializeUIElements()
        {
            if (!TryGetDocument())
            {
                return;
            }

            var root = document.rootVisualElement;
            InitializeSliders(root);
            InitializeButtons(root);
            InitializeContainers(root);
            InitializeTabs(root);
            InitializeSkinColorUI(root);
            InitializeHairColorUI(root);

            // UIVisibilityで使用する要素もキャッシュ
            _mainContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "avatar-customization-container", context: this);
            _buttonContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "reset-button-container", context: this);

            _initialized = true;
        }

        /// <summary>
        /// カラープリセットボタンを作成し、指定されたコンテナに配置する共通メソッド
        /// </summary>
        /// <typeparam name="TColorData">カラープリセットのデータ型</typeparam>
        /// <param name="presetContainer">ボタンを配置する親のVisualElement</param>
        /// <param name="buttonNamePrefix">生成するボタンのname属性のプレフィックス</param>
        /// <param name="presets">カラープリセットデータのリスト</param>
        /// <param name="colorExtractor">TColorDataからUnityEngine.Colorを抽出する関数</param>
        /// <param name="onPresetSelected">プリセットボタンが選択された際に実行するアクション</param>
        /// <returns>生成されたButtonの配列</returns>
        private Button[] CreateAndPlaceColorPresetButtons<TColorData>(
            VisualElement presetContainer,
            string buttonNamePrefix,
            IReadOnlyList<TColorData> presets,
            Func<TColorData, UnityEngine.Color> colorExtractor,
            Action<int> onPresetSelected)
        {
            if (presetContainer == null)
            {
                // 呼び出し元でコンテナのnullチェックとログ出力がされているため、ここでは空配列を返す
                return Array.Empty<Button>();
            }

            presetContainer.Clear(); // 既存の要素をクリア
            var buttons = new Button[presets.Count];

            for (int i = 0; i < presets.Count; i++)
            {
                TColorData presetData = presets[i];
                UnityEngine.Color unityColor = colorExtractor(presetData);

                var button = new Button() { name = $"{buttonNamePrefix}-{i}" };
                button.AddToClassList("color-preset-button");
                button.style.backgroundColor = new StyleColor(unityColor);

                int index = i; // クロージャ用にインデックスをキャプチャ
                button.clicked += () => onPresetSelected(index);

                presetContainer.Add(button);
                buttons[i] = button;
            }
            return buttons;
        }

        /// <summary>
        /// 肌色UI要素の初期化
        /// </summary>
        private void InitializeSkinColorUI(VisualElement root)
        {
            _skinColorContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "skin-color-container", context: this);
            if (_skinColorContainer != null)
            {
                var presetContainer = UIElementUtils.QueryAndCheck<VisualElement>(_skinColorContainer, elementName: "skin-color-preset-container", context: this);
                if (presetContainer != null)
                {
                    _skinColorPresetButtons = CreateAndPlaceColorPresetButtons<SkinColor>(
                        presetContainer,
                        "skin-color-preset",
                        _skinColorPresets,
                        preset => new UnityEngine.Color(preset.Value.R, preset.Value.G, preset.Value.B, preset.Value.A),
                        OnSkinColorPresetSelected
                    );
                }
                else
                {
                    Debug.LogError("[AvatarSystemPage] skin-color-preset-container が見つかりません");
                }
            }
            else
            {
                Debug.LogError("[AvatarSystemPage] skin-color-container が見つかりません");
            }
        }

        /// <summary>
        /// 髪色UI要素の初期化
        /// </summary>
        /// <param name="root">ルート</param>
        private void InitializeHairColorUI(VisualElement root)
        {
            _hairColorContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "hair-color-container", context: this);
            if (_hairColorContainer != null)
            {
                var presetContainer = UIElementUtils.QueryAndCheck<VisualElement>(_hairColorContainer, elementName: "hair-color-preset-container", context: this);
                if (presetContainer != null)
                {
                    _hairColorPresetButtons = CreateAndPlaceColorPresetButtons<HairColor>(
                        presetContainer,
                        "hair-color-preset",
                        _hairColorPresets,
                        preset => new UnityEngine.Color(preset.Value.R, preset.Value.G, preset.Value.B, preset.Value.A),
                        OnHairColorPresetSelected
                    );
                }
                else
                {
                    Debug.LogError("[AvatarSystemPage] hair-color-preset-container が見つかりません");
                }
            }
            else
            {
                Debug.LogError("[AvatarSystemPage] hair-color-container が見つかりません");
            }
        }

        /// <summary>
        /// 肌色プリセットが選択された時の処理
        /// </summary>
        private void OnSkinColorPresetSelected(int index)
        {
            Debug.Log($"[AvatarSystemPage] 肌色プリセット選択: {index}");
            if (index >= 0 && index < _skinColorPresets.Length)
            {
                var selectedColor = _skinColorPresets[index];
                Debug.Log($"[AvatarSystemPage] 肌色変更: R:{selectedColor.Value.R} G:{selectedColor.Value.G} B:{selectedColor.Value.B}");
                _skinColorProperty?.OnNext(selectedColor);
            }
        }

        /// <summary>
        /// 髪色プリセットが選択された時の処理
        /// </summary>
        private void OnHairColorPresetSelected(int index)
        {
            Debug.Log($"[AvatarSystemPage] 髪色プリセット選択: {index}");
            if (index >= 0 && index < _hairColorPresets.Length)
            {
                var selectedColor = _hairColorPresets[index];
                Debug.Log($"[AvatarSystemPage] 髪色変更: R:{selectedColor.Value.R} G:{selectedColor.Value.G} B:{selectedColor.Value.B}");
                _hairColorProperty?.OnNext(selectedColor);
            }
        }


        /// <summary>
        /// UIDocumentを取得
        /// </summary>
        private bool TryGetDocument()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
                if (document == null)
                {
                    Debug.LogError("[AvatarSystemPage] UIDocumentコンポーネントが見つかりません。このGameObjectにUIDocumentをアタッチしてください。");
                    return false;
                }
            }

            if (document.rootVisualElement == null)
            {
                Debug.LogError("[AvatarSystemPage] UIDocumentのrootVisualElementがnullです。UIドキュメントが正しく設定されているか確認してください。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// スライダーの初期化
        /// </summary>
        /// <param name="root">ルート</param>
        private void InitializeSliders(VisualElement root)
        {
            var heightSliderContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "height-slider-container", context: this);
            _heightSlider = UIElementUtils.QueryAndCheck<Slider>(heightSliderContainer, elementName: "slider-control", context: this);

            var shoulderWidthSliderContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "shoulder-width-slider-container", context: this);
            _shoulderWidthSlider = UIElementUtils.QueryAndCheck<Slider>(shoulderWidthSliderContainer, elementName: "slider-control", context: this);

            var bodyWidthSliderContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "body-width-slider-container", context: this);
            _bodyWidthSlider = UIElementUtils.QueryAndCheck<Slider>(bodyWidthSliderContainer, elementName: "slider-control", context: this);

            var headSizeSliderContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "head-size-slider-container", context: this);
            _headSizeSlider = UIElementUtils.QueryAndCheck<Slider>(headSizeSliderContainer, elementName: "slider-control", context: this);
        }

        /// <summary>
        /// ボタンの初期化
        /// </summary>
        /// <param name="root">ルート</param>
        private void InitializeButtons(VisualElement root)
        {
            _resetButton = UIElementUtils.QueryAndCheck<Button>(root, elementName: "reset-button", context: this);
            _saveButton = UIElementUtils.QueryAndCheck<Button>(root, elementName: "save-button", context: this);
            _logoutButton = UIElementUtils.QueryAndCheck<Button>(root, elementName: "logout-button", context: this);
            _navigateToRoomButton = UIElementUtils.QueryAndCheck<Button>(root, elementName: "navigate-to-room-button", context: this);
        }

        /// <summary>
        /// コンテナの初期化
        /// </summary>
        /// <param name="root">ルート</param>
        private void InitializeContainers(VisualElement root)
        {
            _contentContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "content-container", context: this);
            _tabBarContainer = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "tab-bar-container", context: this);
            _tabBarScrollContent = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "tab-bar-scroll-content", context: this);

            // スクロールコンテンツをTabHandlerに設定
            if (_tabHandler != null && _tabBarScrollContent != null)
            {
                _tabHandler.SetScrollContent(_tabBarScrollContent);
            }
        }

        /// <summary>
        /// タブの初期化
        /// </summary>
        /// <param name="root">ルート</param>
        private void InitializeTabs(VisualElement root)
        {
            _bodyTab = UIElementUtils.QueryAndCheck<Button>(root, elementName: "body-tab", context: this);
            _faceTab = UIElementUtils.QueryAndCheck<Button>(root, elementName: "face-tab", context: this);
            // Hair tab has been removed as it's been merged with face tab
        }

        /// <summary>
        /// リスナーのセットアップ
        /// </summary>
        private void SetupListeners()
        {
            if (!_initialized)
            {
                Debug.LogError("[AvatarSystemPage] InitializeUIElementsが完了する前にSetupListenersが呼び出されました。");
                return;
            }

            _disposables = new CompositeDisposable();

            // 各Setupメソッドは内部でnullチェックを行うべき
            SetupSliderListeners();
            SetupButtonListeners();
            SetupTabListeners();
            SetupSkinColorListeners();
            SetupHairColorListeners();
            SetupContentListeners();
        }

        /// <summary>
        /// スライダーのリスナーのセットアップ
        /// </summary>
        private void SetupSliderListeners()
        {
            // スライダーとPropertyをペアとして扱い、一括で処理
            var sliderPairs = new[]
            {
                new { Slider = _heightSlider, Property = _heightProperty, Name = "身長" },
                new { Slider = _shoulderWidthSlider, Property = _shoulderWidthProperty, Name = "肩幅" },
                new { Slider = _bodyWidthSlider, Property = _bodyWidthProperty, Name = "体幅" },
                new { Slider = _headSizeSlider, Property = _headSizeProperty, Name = "頭サイズ" }
            };

            // null要素がないか確認
            if (sliderPairs.Any(pair => pair.Slider == null))
            {
                Debug.LogError("[AvatarSystemPage] 一部のスライダーが初期化されていないため、リスナーを設定できません。InitializeSlidersのログを確認してください。");
                return;
            }

            // すべてのスライダーペアを一括で処理
            foreach (var pair in sliderPairs)
            {
                // スライダーの値変更をReactivePropertyに反映
                Observable.FromEvent<EventCallback<ChangeEvent<float>>, float>(
                    h => (e) => h(e.newValue),
                    h => pair.Slider.RegisterValueChangedCallback(h),
                    h => pair.Slider.UnregisterValueChangedCallback(h))
                    .Subscribe(value =>
                    {
                        Debug.Log($"[AvatarSystemPage] {pair.Name}スライダー値変更: {value}");
                        if (pair.Property != null) pair.Property.OnNext(value);
                    })
                    .AddTo(_disposables);

                // ReactivePropertyの変更をスライダーUIに反映（ループ防止のためSetValueWithoutNotifyを使用）
                if (pair.Property != null)
                {
                    pair.Property
                        .Subscribe(value =>
                        {
                            Debug.Log($"[AvatarSystemPage] {pair.Name}プロパティ値変更: {value}");
                            pair.Slider.SetValueWithoutNotify(value);
                        })
                        .AddTo(_disposables);
                }
            }
        }

        /// <summary>
        /// ボタンのリスナーのセットアップ
        /// </summary>
        private void SetupButtonListeners()
        {
            // 一度に複数のボタンを処理するための構造
            var buttonSetups = new[]
            {
                new { Button = _resetButton, Action = new Action(() => _resetRequestedSubject?.OnNext(Unit.Default)) },
                new { Button = _saveButton, Action = new Action(() => HandleSaveClicked()) },
                new { Button = _logoutButton, Action = new Action(() => {
                    Debug.Log("[AvatarSystemPage] ログアウトボタンがクリックされました。");
                    _logoutRequestedSubject?.OnNext(Unit.Default);
                }) },
                new { Button = _navigateToRoomButton, Action = new Action(() => {
                    Debug.Log("[AvatarSystemPage] ルームへ移動ボタンがクリックされました。");
                    _navigateToRoomRequestedSubject?.OnNext(Unit.Default);
                }) }
            };

            // 一括処理
            foreach (var setup in buttonSetups)
            {
                if (setup.Button != null)
                {
                    Observable.FromEvent(
                        h => setup.Button.clicked += h,
                        h => setup.Button.clicked -= h)
                        .Subscribe(_ => setup.Action())
                        .AddTo(_disposables);
                }
            }
        }

        /// <summary>
        /// タブのリスナーのセットアップ
        /// </summary>
        private void SetupTabListeners()
        {
            // タブボタンとページのマッピングを作成
            var tabMapping = new[]
            {
                new { Button = _bodyTab, PageIndex = 0 },
                new { Button = _faceTab, PageIndex = 1 }
            };

            // 各タブにイベントリスナーを設定
            foreach (var tab in tabMapping)
            {
                if (tab.Button != null)
                {
                    var pageIndex = tab.PageIndex; // クロージャのためにローカルコピー
                    Observable.FromEvent(
                        h => tab.Button.clicked += h,
                        h => tab.Button.clicked -= h)
                        .Subscribe(_ => ChangePage(pageIndex))
                        .AddTo(_disposables);
                }
            }

            // タブバーのスワイプイベント
            if (_tabBarScrollContent != null && _tabHandler != null)
            {
                _tabBarScrollContent.RegisterCallback<PointerDownEvent>(_tabHandler.OnPointerDown);
                _tabBarScrollContent.RegisterCallback<PointerMoveEvent>(_tabHandler.OnPointerMove);
                _tabBarScrollContent.RegisterCallback<PointerUpEvent>(_tabHandler.OnPointerUp);

                // TabHandlerからのページ変更イベント
                _tabHandler.OnPageChanged
                    .Subscribe(ChangePage)
                    .AddTo(_disposables);
            }
            else
            {
                if (_tabBarScrollContent == null)
                {
                    // QueryAndCheck がエラーログを出力済み
                }
                if (_tabHandler == null)
                {
                    Debug.LogError("[AvatarSystemPage] TabHandlerがnullのため、スワイプリスナーを設定できません。");
                }
            }
        }

        /// <summary>
        /// コンテンツのリスナーのセットアップ
        /// </summary>
        private void SetupContentListeners()
        {
            // QueryAndCheckで取得済みなので、nullチェックを行う
            if (_contentContainer == null || _contentHandler == null)
            {
                if (_contentContainer == null) { /* QueryAndCheck がエラーログを出力済み */ }
                if (_contentHandler == null) { Debug.LogError("[AvatarSystemPage] ContentHandlerがnullのため、コンテンツリスナーを設定できません。"); }
                return;
            }

            _contentHandler.SetContentContainer(_contentContainer);
            _contentHandler.SetMaxPages(NumberOfTabs); // ページ数を設定

            // ContentHandlerからのページ変更イベント
            _contentHandler.OnPageChanged
                .Subscribe(page =>
                {
                    ChangePage(page);
                })
                .AddTo(_disposables);

            // コンテンツエリアのスワイプイベント
            _contentContainer.RegisterCallback<PointerDownEvent>(_contentHandler.OnPointerDown);
            _contentContainer.RegisterCallback<PointerMoveEvent>(_contentHandler.OnPointerMove);
            _contentContainer.RegisterCallback<PointerUpEvent>(_contentHandler.OnPointerUp);
        }

        /// <summary>
        /// スキンカラーリスナーのセットアップ
        /// </summary>
        private void SetupSkinColorListeners()
        {
            // 必要なプロパティや配列がnullでないか確認
            if (_skinColorProperty == null || _skinColorPresetButtons == null)
            {
                Debug.LogError("[AvatarSystemPage] SkinColorプロパティまたはボタンがnullです。リスナーを設定できません。");
                return;
            }

            // ReactivePropertyの変更を購読し、UI更新メソッドを呼び出す
            _skinColorProperty
                .Subscribe(UpdateSkinColorSelectionUI)
                .AddTo(_disposables);
        }

        /// <summary>
        /// 肌色プリセットボタンの選択状態UIを更新します。
        /// </summary>
        /// <param name="newColor">新しく選択された肌色</param>
        private void UpdateSkinColorSelectionUI(SkinColor newColor)
        {
            // 配列がnullまたは空でないことを確認 (追加の安全チェック)
            if (_skinColorPresets == null || _skinColorPresetButtons == null)
            {
                return;
            }

            // ループで各ボタンの選択状態を更新
            for (int i = 0; i < _skinColorPresetButtons.Length; i++)
            {
                Button button = _skinColorPresetButtons[i];
                // ボタンがnullでないこと、およびインデックスがプリセット配列の範囲内であることを確認
                if (button != null && i < _skinColorPresets.Length)
                {
                    // 現在のボタンが選択された色に対応するかどうかを判断
                    bool isSelected = _skinColorPresets[i].Equals(newColor);
                    // isSelected の値に基づいて選択クラスを追加/削除
                    button.EnableInClassList("color-preset-button--selected", isSelected); // クラス名を共通スタイルに変更
                }
            }
        }

        /// <summary>
        /// ヘアカラーリスナーのセットアップ
        /// </summary>
        private void SetupHairColorListeners()
        {
            // 必要なプロパティや配列がnullでないか確認
            if (_hairColorProperty == null || _hairColorPresetButtons == null)
            {
                Debug.LogError("[AvatarSystemPage] HairColorプロパティまたはボタンがnullです。リスナーを設定できません。");
                return;
            }

            // ReactivePropertyの変更を購読し、UI更新メソッドを呼び出す
            _hairColorProperty
                .Subscribe(UpdateHairColorSelectionUI)
                .AddTo(_disposables);
        }

        /// <summary>
        /// 髪色プリセットボタンの選択状態UIを更新します。
        /// </summary>
        /// <param name="newColor">新しく選択された髪色</param>
        private void UpdateHairColorSelectionUI(HairColor newColor)
        {
            // 配列がnullまたは空でないことを確認 (追加の安全チェック)
            if (_hairColorPresets == null || _hairColorPresetButtons == null)
            {
                return;
            }

            // ループで各ボタンの選択状態を更新
            for (int i = 0; i < _hairColorPresetButtons.Length; i++)
            {
                Button button = _hairColorPresetButtons[i];
                // ボタンがnullでないこと、およびインデックスがプリセット配列の範囲内であることを確認
                if (button != null && i < _hairColorPresets.Length)
                {
                    // 現在のボタンが選択された色に対応するかどうかを判断
                    bool isSelected = _hairColorPresets[i].Equals(newColor);
                    // isSelected の値に基づいて選択クラスを追加/削除
                    button.EnableInClassList("color-preset-button--selected", isSelected); // クラス名を共通スタイルに変更
                }
            }
        }

        /// <summary>
        /// ページを変更し、関連するUI更新と通知を行う中央メソッド
        /// </summary>
        /// <param name="newPageIndex">表示する新しいページのインデックス</param>
        private void ChangePage(int newPageIndex)
        {
            // 無限ループや不要な処理を防ぐ
            if (_isSwitchingTabs) return;
            if (newPageIndex == _currentPageIndex && _initialized) return; // Don't re-process if already on the page (allow initial call)

            _isSwitchingTabs = true;
            try
            {
                _currentPageIndex = newPageIndex;

                // ハンドラーにページ変更を指示 (視覚的更新のため)
                _tabHandler?.SwitchToPage(newPageIndex);
                _contentHandler?.SwitchToPage(newPageIndex);

                // UI要素（タブハイライト）を更新
                UpdateTabSelection(newPageIndex);

                // Presenterに通知
                _tabChangedSubject?.OnNext(newPageIndex);
            }
            finally
            {
                _isSwitchingTabs = false;
            }
        }

        /// <summary>
        /// 非UI領域でのポインターアップ
        /// </summary>
        public void HandleNonUIPointerUp(Vector2 position)
        {
            // 初期化チェックを追加
            if (!_initialized)
            {
                Debug.Log("[AvatarSystemPage] 未初期化のためポインターイベントを処理できません。");
                return;
            }

            ToggleUIVisibility();
        }

        /// <summary>
        /// 保存ボタンがクリックされた時の処理
        /// </summary>
        private void HandleSaveClicked()
        {
            if (!_initialized) return;

            Debug.Log("[AvatarSystemPage] 保存ボタンがクリックされました。");
            _saveRequestedSubject?.OnNext(Unit.Default);
        }

        /// <summary>
        /// UIの値を更新
        /// </summary>
        /// <param name="height">身長</param>
        /// <param name="shoulderWidth">肩幅</param>
        /// <param name="bodyWidth">体の横幅</param>
        /// <param name="headSize">頭の大きさ</param>
        /// <param name="skinColor">肌の色</param>
        /// <param name="hairColor">髪の色</param>
        public void UpdateUIValues(float height, float shoulderWidth, float bodyWidth, float headSize, SkinColor skinColor, HairColor hairColor)
        {
            if (!_initialized)
            {
                return;
            }

            // Basic Properties (Sliders)
            _heightProperty?.OnNext(height);
            _shoulderWidthProperty?.OnNext(shoulderWidth);
            _bodyWidthProperty?.OnNext(bodyWidth);
            _headSizeProperty?.OnNext(headSize);

            // Colors
            _skinColorProperty?.OnNext(skinColor);
            _hairColorProperty?.OnNext(hairColor);
        }

        /// <summary>
        /// 保存ボタンの状態を更新
        /// </summary>
        /// <param name="hasChanges">変更があるかどうか</param>
        public void UpdateSaveButtonState(bool hasChanges)
        {
            if (!_initialized || _saveButton == null)
            {
                Debug.Log($"[AvatarSystemPage] 保存ボタンの状態を更新できません。初期化: {_initialized}, ボタン存在: {_saveButton != null}");
                return;
            }

            // ユースケースの状態に従う - 一度だけ操作することでコードを効率化
            if (_saveButton.enabledSelf != hasChanges)
            {
                _saveButton.SetEnabled(hasChanges);
                _saveButton.style.opacity = hasChanges ? 1.0f : 0.5f;
            }
        }

        /// <summary>
        /// UIの表示/非表示を切り替える
        /// </summary>
        public void ToggleUIVisibility()
        {
            // 初期化チェックを追加
            if (!_initialized || document == null || document.rootVisualElement == null)
            {
                Debug.Log("[AvatarSystemPage] 初期化されていないためUIの表示状態を切り替えできません。");
                return;
            }

            // クールダウン時間内の場合は何もしない（連打防止）
            if (Time.time - _lastUIToggleTime < UI_TOGGLE_COOLDOWN)
            {
                return;
            }

            _lastUIToggleTime = Time.time;
            SetUIVisibility(!_isUIVisible);
        }

        /// <summary>
        /// UIの表示/非表示を設定する
        /// </summary>
        /// <param name="visible">表示するかどうか</param>
        public void SetUIVisibility(bool visible)
        {
            // 状態が同じなら何もしない（早期リターン）
            if (_isUIVisible == visible)
            {
                return;
            }

            _lastUIToggleTime = Time.time; // 設定時もクールダウンタイマーをリセット
            _isUIVisible = visible;
            UpdateUIVisibility();
            _uiVisibilityChangedSubject?.OnNext(_isUIVisible);
        }

        /// <summary>
        /// UIの表示状態を更新する
        /// </summary>
        private void UpdateUIVisibility()
        {
            if (!_initialized || document == null || document.rootVisualElement == null)
            {
                Debug.Log("[AvatarSystemPage] UI表示状態を更新できません: 未初期化またはルート要素が見つかりません。");
                return;
            }

            var displayStyle = _isUIVisible ? DisplayStyle.Flex : DisplayStyle.None;
            var pickingMode = _isUIVisible ? PickingMode.Position : PickingMode.Ignore;

            // NULL検査を一回でまとめて行い、条件分岐を減らす
            if (_mainContainer != null) _mainContainer.style.display = displayStyle;
            else Debug.LogError("[AvatarSystemPage] メインコンテナが見つかりません。");

            if (_buttonContainer != null) _buttonContainer.style.display = displayStyle;
            else
            {
                Debug.Log("[AvatarSystemPage] ボタンコンテナが見つからないため、個別のボタンを操作します。");
                // 配列にまとめて操作
                Button[] buttons = { _resetButton, _saveButton, _logoutButton, _navigateToRoomButton };
                foreach (var button in buttons)
                {
                    if (button != null) button.style.display = displayStyle;
                }
            }

            // スワイプ入力を受け付けるための要素もUIが非表示の場合は無効化
            if (_tabBarScrollContent != null) _tabBarScrollContent.pickingMode = pickingMode;
            else Debug.LogError("[AvatarSystemPage] タブバースクロールコンテンツが見つかりません。");

            if (_contentContainer != null) _contentContainer.pickingMode = pickingMode;
            else Debug.LogError("[AvatarSystemPage] コンテンツコンテナが見つかりません。");
        }

        /// <summary>
        /// 無効になった時の処理
        /// </summary>
        private void OnDisable()
        {
            // 全てのリソースを一度で解放
            CleanupResources();

            // 状態をリセット
            _initialized = false;
        }

        /// <summary>
        /// タブ関連のコールバックを解除
        /// </summary>
        private void UnregisterTabCallbacks()
        {
            if (_tabBarScrollContent == null || _tabHandler == null) return;

            // 一括で登録解除
            _tabBarScrollContent.UnregisterCallback<PointerDownEvent>(_tabHandler.OnPointerDown);
            _tabBarScrollContent.UnregisterCallback<PointerMoveEvent>(_tabHandler.OnPointerMove);
            _tabBarScrollContent.UnregisterCallback<PointerUpEvent>(_tabHandler.OnPointerUp);
        }

        /// <summary>
        /// コンテンツ関連のコールバックを解除
        /// </summary>
        private void UnregisterContentCallbacks()
        {
            if (_contentContainer == null || _contentHandler == null) return;

            // 一括で登録解除
            _contentContainer.UnregisterCallback<PointerDownEvent>(_contentHandler.OnPointerDown);
            _contentContainer.UnregisterCallback<PointerMoveEvent>(_contentHandler.OnPointerMove);
            _contentContainer.UnregisterCallback<PointerUpEvent>(_contentHandler.OnPointerUp);
        }

        /// <summary>
        /// リソースのクリーンアップを行う共通メソッド
        /// </summary>
        private void CleanupResources()
        {
            // Disposeが確実に行われるように
            _disposables?.Dispose();
            _disposables = null;

            // 登録解除
            UnregisterTabCallbacks();
            UnregisterContentCallbacks();
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        private void OnDestroy()
        {
            // リソース解放
            CleanupResources();

            // ハンドラーもDispose
            _tabHandler?.Dispose();
            _contentHandler?.Dispose();
        }

        /// <summary>
        /// タブの選択を更新
        /// </summary>
        /// <param name="currentPage">現在選択されているべきページのインデックス</param>
        private void UpdateTabSelection(int currentPage)
        {
            // すべてのタブを一度に配列で処理
            Button[] allTabs = { _bodyTab, _faceTab };

            // 全タブから選択クラスを削除
            foreach (var tab in allTabs)
            {
                tab?.RemoveFromClassList("selected");
            }

            // インデックスが有効範囲内かチェック
            if (currentPage >= 0 && currentPage < allTabs.Length && allTabs[currentPage] != null)
            {
                allTabs[currentPage].AddToClassList("selected");
            }
        }

        /// <summary>
        /// Input Handlerのイベントを購読する
        /// </summary>
        private void SubscribeToInputHandlerEvents()
        {
            if (_inputHandler == null || _disposables == null)
            {
                Debug.LogError("[AvatarSystemPage] InputHandlerまたはDisposablesがInputHandlerイベント購読の準備ができていません。");
                return;
            }

            // Pointer Down -> ドラッグ開始の可能性 (InputHandlerが内部で処理)
            _inputHandler.PointerDown
                .Where(pos => !_inputHandler.IsPointerOverUI(pos))
                .Subscribe(pos =>
                {
                    // StartGlobalDragging はハンドラー内部で処理される
                    // タップとドラッグ開始を区別する必要がある場合は、ここにロジックが必要になる可能性あり
                })
                .AddTo(_disposables);

            // Pointer Move -> ドラッグ中ならカメラを回転
            _inputHandler.PointerMove
                // ドラッグ中(RPから取得)の場合のみ反応
                .Where(_ => _inputHandler.IsDragging.CurrentValue)
                // マルチタップ中は回転しないように、タッチ数が1以下の場合のみ回転を許可
                .Where(_ => _inputHandler.GetTouchCount() <= 1)
                // UI操作を除外
                .Where(pos => !_inputHandler.IsPointerOverUI(pos))
                .Subscribe(currentPos =>
                {
                    // リアクティブプロパティを使用して差分を計算
                    Vector2 previousPos = _inputHandler.PreviousPointerPosition.CurrentValue;
                    Vector2 delta = currentPos - previousPos;

                    // 水平方向と垂直方向の移動量を分離
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        // 主に水平方向の移動の場合、カメラ回転イベントを発行
                        _cameraRotationDeltaSubject?.OnNext(new Vector2(delta.x, 0));
                    }
                    else
                    {
                        // 主に垂直方向の移動の場合、カメラ高さ変更イベントを発行
                        _cameraHeightDeltaSubject?.OnNext(delta.y);
                    }
                })
                .AddTo(_disposables);

            // Pointer Up -> 非UI領域でのタップ/クリックを通知
            _inputHandler.PointerUp
                .Where(pos => !_inputHandler.IsPointerOverUI(pos))
                .Subscribe(pos =>
                {
                    // 短いドラッグ（タップ）かどうかを判定
                    if (_inputHandler.IsShortDrag)
                    {
                        // 非UI領域でのポインターアップを通知 (UIトグルなど)
                        HandleNonUIPointerUp(pos);
                    }
                })
                .AddTo(_disposables);

            // ズームイベントの購読
            _inputHandler.OnPinch
                .Subscribe(pinchDelta =>
                {
                    _cameraZoomRequestedSubject?.OnNext(pinchDelta);
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 破棄可能オブジェクトを初期化します。
        /// </summary>
        private void InitializeDisposables()
        {
            var disposables = new IDisposable[] {
                // Subjects
                _resetRequestedSubject,
                _saveRequestedSubject,
                _logoutRequestedSubject,
                _navigateToRoomRequestedSubject,
                _nonUIPointerUpSubject,
                _uiVisibilityChangedSubject,
                _tabChangedSubject,
                _cameraRotationDeltaSubject,
                _cameraZoomRequestedSubject,
                _cameraHeightDeltaSubject,

                // Reactive Properties
                _heightProperty,
                _shoulderWidthProperty,
                _bodyWidthProperty,
                _headSizeProperty,
                _skinColorProperty,
                _hairColorProperty
            };

            foreach (var disposable in disposables)
            {
                _disposables.Add(disposable);
            }
        }
    }
}
