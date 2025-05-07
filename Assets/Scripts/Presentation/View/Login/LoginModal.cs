using UnityEngine;
using UnityEngine.UIElements;
using R3;

using Presentation.Utils;

namespace Presentation.View
{
    /// <summary>
    /// ログインモーダル
    /// </summary>
    /// <remarks>
    /// ログインモーダル
    /// </remarks>
    public sealed class LoginModal : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        private VisualElement _modal;
        private VisualElement _loginForm;
        private VisualElement _registerForm;

        private TextField _emailField;
        private TextField _passwordField;
        private TextField _registerEmailField;
        private TextField _registerPasswordField;
        private TextField _confirmPasswordField;

        private Button _loginButton;
        private Button _loginCancelButton;
        private Button _registerButton;
        private Button _registerCancelButton;
        private Button _loginTab;
        private Button _registerTab;

        private Label _loginErrorLabel;
        private Label _registerErrorLabel;

        // R3 Subjects
        private Subject<(string email, string password)> _loginRequestedSubject;
        private Subject<(string email, string password)> _registerRequestedSubject;
        private Subject<Unit> _cancelRequestedSubject;

        // Observable properties
        public Observable<(string email, string password)> OnLoginRequested
            => _loginRequestedSubject ??= new Subject<(string email, string password)>();
        public Observable<(string email, string password)> OnRegisterRequested
            => _registerRequestedSubject ??= new Subject<(string email, string password)>();
        public Observable<Unit> OnCancelRequested
            => _cancelRequestedSubject ??= new Subject<Unit>();

        // Disposable container for all subscriptions
        private CompositeDisposable _disposables = new();

        /// <summary>
        /// OnEnable
        /// </summary>
        private void OnEnable()
        {
            InitializeUIElements();
            SetupListeners();
        }

        /// <summary>
        /// InitializeUIElements
        /// </summary>
        private void InitializeUIElements()
        {
            if (document == null) document = GetComponent<UIDocument>();

            if (document == null || document.rootVisualElement == null)
            {
                Debug.LogError("UIDocument or its root element is null.");
                return;
            }

            var root = document.rootVisualElement;
            _modal = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "login-modal", context: this);
            if (_modal == null) return;

            var modalContent = UIElementUtils.QueryAndCheck<VisualElement>(_modal, elementName: "modal-content", context: this);
            if (modalContent == null) return; // モーダルコンテンツコンテナがない場合は処理を続けない

            // ログインフォーム
            _loginForm = UIElementUtils.QueryAndCheck<VisualElement>(modalContent, elementName: "login-form", context: this);

            // インプットフィールド
            var emailContainer = UIElementUtils.QueryAndCheck<VisualElement>(_loginForm, elementName: "email-field-container", context: this);
            _emailField = UIElementUtils.QueryAndCheck<TextField>(emailContainer, elementName: "input-field", context: this);

            var passwordContainer = UIElementUtils.QueryAndCheck<VisualElement>(_loginForm, elementName: "password-field-container", context: this);
            _passwordField = UIElementUtils.QueryAndCheck<TextField>(passwordContainer, elementName: "input-field", context: this);

            // エラーラベル
            _loginErrorLabel = UIElementUtils.QueryAndCheck<Label>(_loginForm, elementName: "login-error-label", context: this);

            // ボタン
            var loginButtonContainer = UIElementUtils.QueryAndCheck<VisualElement>(_loginForm, elementName: "login-button-container", context: this);
            if (loginButtonContainer != null) // コンテナが存在する場合のみボタンをクエリする
            {
                _loginButton = UIElementUtils.QueryAndCheck<Button>(loginButtonContainer, elementName: "login-button", context: this);
                _loginCancelButton = UIElementUtils.QueryAndCheck<Button>(loginButtonContainer, elementName: "login-cancel-button", context: this);
            }

            // 新規登録フォーム
            _registerForm = UIElementUtils.QueryAndCheck<VisualElement>(modalContent, elementName: "register-form", context: this);

            // 登録用インプットフィールド
            var registerEmailContainer = UIElementUtils.QueryAndCheck<VisualElement>(_registerForm, elementName: "register-email-field-container", context: this);
            _registerEmailField = UIElementUtils.QueryAndCheck<TextField>(registerEmailContainer, elementName: "input-field", context: this);

            var registerPasswordContainer = UIElementUtils.QueryAndCheck<VisualElement>(_registerForm, elementName: "register-password-field-container", context: this);
            _registerPasswordField = UIElementUtils.QueryAndCheck<TextField>(registerPasswordContainer, elementName: "input-field", context: this);

            var confirmPasswordContainer = UIElementUtils.QueryAndCheck<VisualElement>(_registerForm, elementName: "confirm-password-field-container", context: this);
            _confirmPasswordField = UIElementUtils.QueryAndCheck<TextField>(confirmPasswordContainer, elementName: "input-field", context: this);

            // 登録エラーラベル
            _registerErrorLabel = UIElementUtils.QueryAndCheck<Label>(_registerForm, elementName: "register-error-label", context: this);

            // 登録ボタン
            var registerButtonContainer = UIElementUtils.QueryAndCheck<VisualElement>(_registerForm, elementName: "register-button-container", context: this);
            if (registerButtonContainer != null) // コンテナが存在する場合のみボタンをクエリする
            {
                _registerButton = UIElementUtils.QueryAndCheck<Button>(registerButtonContainer, elementName: "register-button", context: this);
                _registerCancelButton = UIElementUtils.QueryAndCheck<Button>(registerButtonContainer, elementName: "register-cancel-button", context: this);
            }

            // タブを検索する
            _loginTab = UIElementUtils.QueryAndCheck<Button>(modalContent, elementName: "login-tab", context: this);
            _registerTab = UIElementUtils.QueryAndCheck<Button>(modalContent, elementName: "register-tab", context: this);

            // デフォルトでは非表示
            _modal.style.display = DisplayStyle.None;
            _loginErrorLabel.style.display = DisplayStyle.None;
            _registerErrorLabel.style.display = DisplayStyle.None;
            _registerForm.style.display = DisplayStyle.None;
            _loginForm.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// リスナーの設定
        /// </summary>
        private void SetupListeners()
        {
            if (_loginButton == null || _loginCancelButton == null ||
                _registerButton == null || _registerCancelButton == null ||
                _loginTab == null || _registerTab == null) return;

            // ログインタブとボタンのイベント
            Observable.FromEvent(
                h => _loginButton.clicked += h,
                h => _loginButton.clicked -= h)
                .Subscribe(_ => HandleLoginClicked())
                .AddTo(_disposables);

            Observable.FromEvent(
                h => _loginCancelButton.clicked += h,
                h => _loginCancelButton.clicked -= h)
                .Subscribe(_ => HandleCancelClicked())
                .AddTo(_disposables);

            // 新規登録タブとボタンのイベント
            Observable.FromEvent(
                h => _registerButton.clicked += h,
                h => _registerButton.clicked -= h)
                .Subscribe(_ => HandleRegisterClicked())
                .AddTo(_disposables);

            Observable.FromEvent(
                h => _registerCancelButton.clicked += h,
                h => _registerCancelButton.clicked -= h)
                .Subscribe(_ => HandleCancelClicked())
                .AddTo(_disposables);

            // タブ切り替えイベント
            Observable.FromEvent(
                h => _loginTab.clicked += h,
                h => _loginTab.clicked -= h)
                .Subscribe(_ => SwitchToLoginTab())
                .AddTo(_disposables);

            Observable.FromEvent(
                h => _registerTab.clicked += h,
                h => _registerTab.clicked -= h)
                .Subscribe(_ => SwitchToRegisterTab())
                .AddTo(_disposables);
        }

        /// <summary>
        /// ログインタブに切り替える
        /// </summary>
        private void SwitchToLoginTab()
        {
            _loginForm.style.display = DisplayStyle.Flex;
            _registerForm.style.display = DisplayStyle.None;
            _loginTab.AddToClassList("tab-active");
            _registerTab.RemoveFromClassList("tab-active");
            _loginErrorLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// 新規登録タブに切り替える
        /// </summary>
        private void SwitchToRegisterTab()
        {
            _loginForm.style.display = DisplayStyle.None;
            _registerForm.style.display = DisplayStyle.Flex;
            _loginTab.RemoveFromClassList("tab-active");
            _registerTab.AddToClassList("tab-active");
            _registerErrorLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// ログインボタンがクリックされたときの処理
        /// </summary>
        private void HandleLoginClicked()
        {
            string email = _emailField.value;
            string password = _passwordField.value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowLoginError("メールアドレスとパスワードを入力してください。");
                return;
            }
            _loginRequestedSubject?.OnNext((email, password));
        }

        /// <summary>
        /// 新規登録ボタンがクリックされたときの処理
        /// </summary>
        private void HandleRegisterClicked()
        {
            string email = _registerEmailField.value;
            string password = _registerPasswordField.value;
            string confirmPassword = _confirmPasswordField.value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ShowRegisterError("すべての項目を入力してください。");
                return;
            }

            if (password != confirmPassword)
            {
                ShowRegisterError("パスワードが一致しません。");
                return;
            }

            if (password.Length < 6)
            {
                ShowRegisterError("パスワードは6文字以上必要です。");
                return;
            }
            _registerRequestedSubject?.OnNext((email, password));
        }

        /// <summary>
        /// キャンセルボタンがクリックされたときの処理
        /// </summary>
        private void HandleCancelClicked()
        {
            Hide();
            _cancelRequestedSubject?.OnNext(Unit.Default);
        }

        /// <summary>
        /// 表示
        /// </summary>
        public void Show()
        {
            _modal.style.display = DisplayStyle.Flex;

            // 初期値をクリアする
            _emailField.value = string.Empty;
            _passwordField.value = string.Empty;
            _registerEmailField.value = string.Empty;
            _registerPasswordField.value = string.Empty;
            _confirmPasswordField.value = string.Empty;

            // エラーメッセージをクリアする
            _loginErrorLabel.style.display = DisplayStyle.None;
            _registerErrorLabel.style.display = DisplayStyle.None;

            // デフォルトではログインタブを表示する
            SwitchToLoginTab();

            // モーダルが表示されたときにフォーカスを設定
            _emailField.Focus();
        }

        /// <summary>
        /// 非表示
        /// </summary>
        public void Hide()
        {
            _modal.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// ログインエラーを表示
        /// </summary>
        public void ShowLoginError(string errorMessage)
        {
            _loginErrorLabel.text = errorMessage;
            _loginErrorLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 新規登録エラーを表示
        /// </summary>
        public void ShowRegisterError(string errorMessage)
        {
            _registerErrorLabel.text = errorMessage;
            _registerErrorLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 新規登録成功を表示
        /// </summary>
        public void ShowRegisterSuccess(string message)
        {
            // ログインタブに切り替えて、新規登録成功メッセージを表示
            SwitchToLoginTab();
            _loginErrorLabel.text = message;
            _loginErrorLabel.style.color = new Color(0, 0.7f, 0);
            _loginErrorLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 非アクティブになったときの処理
        /// </summary>
        private void OnDisable()
        {
            _disposables?.Dispose();
            _disposables = null;
        }
    }
}
