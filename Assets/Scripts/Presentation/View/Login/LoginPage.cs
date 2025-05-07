using UnityEngine;
using UnityEngine.UIElements;
using R3;
using Presentation.ScriptableObjects;
using Presentation.Utils;
using VContainer;

namespace Presentation.View
{
    /// <summary>
    /// ログインページ
    /// </summary>
    /// <remarks>
    /// ログインページ
    /// </remarks>
    public sealed class LoginPage : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private LoginModal loginModal;

        private ApiSettings _apiSettings;

        private Button _loginButton;
        private Button _continueAsGuestButton;
        private Label _versionLabel;
        private Label _appIdLabel;

        // イベント用のReactiveProperty
        private readonly Subject<Unit> _onLoginRequested = new();
        private readonly Subject<Unit> _onContinueAsGuestRequested = new();

        // Observable型として公開
        public Observable<Unit> OnLoginRequested => _onLoginRequested;
        public Observable<Unit> OnContinueAsGuestRequested => _onContinueAsGuestRequested;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="apiSettings">API設定</param>
        [Inject]
        public void Construct(ApiSettings apiSettings)
        {
            _apiSettings = apiSettings;
        }

        /// <summary>
        /// Awake
        /// </summary>
        private void Awake()
        {
            if (document == null)
            {
                Debug.LogError("UIDocument is not assigned!");
                return;
            }

            if (loginModal == null)
            {
                Debug.LogError("LoginModal is not assigned!");
                return;
            }

            if (_apiSettings == null)
            {
                Debug.LogError("ApiSettings was not injected!");
                return;
            }
        }

        /// <summary>
        /// OnEnable
        /// </summary>
        private void OnEnable()
        {
            var root = document.rootVisualElement;
            var loginPage = UIElementUtils.QueryAndCheck<VisualElement>(root, elementName: "login-page", context: this);
            if (loginPage == null) return;

            var buttonContainer = UIElementUtils.QueryAndCheck<VisualElement>(loginPage, elementName: "button-container", context: this);
            if (buttonContainer != null)
            {
                _loginButton = UIElementUtils.QueryAndCheck<Button>(buttonContainer, elementName: "open-login-button", context: this);
                _continueAsGuestButton = UIElementUtils.QueryAndCheck<Button>(buttonContainer, elementName: "continue-as-guest-button", context: this);
            }

            var versionContainer = UIElementUtils.QueryAndCheck<VisualElement>(loginPage, elementName: "version-container", context: this);
            if (versionContainer != null)
            {
                _versionLabel = UIElementUtils.QueryAndCheck<Label>(versionContainer, elementName: "version-label", context: this);
                _appIdLabel = UIElementUtils.QueryAndCheck<Label>(versionContainer, elementName: "app-id-label", context: this);
            }

            if (_loginButton != null)
            {
                _loginButton.clicked += OnLoginButtonClicked;
            }

            if (_continueAsGuestButton != null)
            {
                _continueAsGuestButton.clicked += OnContinueAsGuestButtonClicked;
            }

            // バージョン情報とIDを設定
            UpdateVersionInfo();
        }

        /// <summary>
        /// UpdateVersionInfo
        /// </summary>
        private void UpdateVersionInfo()
        {
            if (_versionLabel != null)
            {
                // ApiSettingsから直接アプリバージョンを取得
                string appVersion = _apiSettings.appVersion;
                _versionLabel.text = $"Version: {appVersion}";
            }

            if (_appIdLabel != null)
            {
                // デバイス固有のIDを取得
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                // デバイスIDは長いので最初の8文字だけ使用
                string shortDeviceId = deviceId.Length > 8 ? deviceId[..8] : deviceId;
                _appIdLabel.text = $"ID: {shortDeviceId}";
            }
        }

        /// <summary>
        /// OnDisable
        /// </summary>
        private void OnDisable()
        {
            if (_loginButton != null)
            {
                _loginButton.clicked -= OnLoginButtonClicked;
            }

            if (_continueAsGuestButton != null)
            {
                _continueAsGuestButton.clicked -= OnContinueAsGuestButtonClicked;
            }
        }

        /// <summary>
        /// OnLoginButtonClicked
        /// </summary>
        private void OnLoginButtonClicked()
        {
            // ログインボタンをクリックした時にモーダルを表示する前にイベントを発行
            _onLoginRequested.OnNext(Unit.Default);
        }

        /// <summary>
        /// OnContinueAsGuestButtonClicked
        /// </summary>
        private void OnContinueAsGuestButtonClicked()
        {
            // ゲストとして続けるボタンをクリックした時にイベントを発行
            _onContinueAsGuestRequested.OnNext(Unit.Default);
        }

        /// <summary>
        /// ShowLoginModal
        /// </summary>
        public void ShowLoginModal()
        {
            loginModal.Show();
        }

        /// <summary>
        /// HideLoginModal
        /// </summary>
        public void HideLoginModal()
        {
            loginModal.Hide();
        }

        /// <summary>
        /// OnDestroy
        /// </summary>
        private void OnDestroy()
        {
            _onLoginRequested.Dispose();
            _onContinueAsGuestRequested.Dispose();
        }
    }
}
