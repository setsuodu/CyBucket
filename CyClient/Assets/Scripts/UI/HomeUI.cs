using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// Unity 6000.5.x PanelRenderer 入口。
/// 不要用 UIDocument / rootVisualElement / 反射。
/// 用 RegisterUIReloadCallback(renderer, root, version)。
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public sealed class HomeUI : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset loginUxml;

    private PanelRenderer _panelRenderer;
    private VisualElement _homeRoot;
    private VisualElement _loginLayer;
    private VisualElement _loginPopup;

    private Button _btnLogin;
    private Button _btnMatch;
    private Label _playerName;
    private Label _netStatus;

    private int _uiVersion = -1;

    private GameSession _session;
    private IPublisher<MatchStartMessage> _matchPub;
    private IPublisher<LoginSuccessMessage> _loginPub;
    private ISubscriber<NetworkStateMessage> _netSub;
    private IDisposable _netBag;

    private void Awake()
    {
        _panelRenderer = GetComponent<PanelRenderer>();
        // 6000.5：在 Awake 注册；不要在 Awake 里 enabled=false 再 enable
        _panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void Start()
    {
        // 从场景 AppLifetimeScope 取服务（避免强制把 HomeUI 做成 EntryPoint）
        var scope = FindObjectOfType<AppLifetimeScope>();
        if (scope == null)
        {
            Debug.LogError("[HomeUI] AppLifetimeScope missing");
            return;
        }

        _session = scope.Container.Resolve<GameSession>();
        _matchPub = scope.Container.Resolve<IPublisher<MatchStartMessage>>();
        _loginPub = scope.Container.Resolve<IPublisher<LoginSuccessMessage>>();
        _netSub = scope.Container.Resolve<ISubscriber<NetworkStateMessage>>();

        _netBag = _netSub.Subscribe(OnNetState);
        RefreshPlayerLabel();
    }

    private void OnDestroy()
    {
        if (_panelRenderer != null)
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);

        UnbindHomeEvents();
        UnbindLoginEvents();
        _netBag?.Dispose();

        _loginPopup = null;
        _loginLayer = null;
        _homeRoot = null;
    }

    /// <summary>
    /// 6000.5.x 正式入口。version 用于避免动态 UI 在 reload 时累积。
    /// </summary>
    private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        if (_uiVersion == version)
            return;

        _uiVersion = version;

        UnbindHomeEvents();
        UnbindLoginEvents();
        _loginPopup = null;

        _homeRoot = root;
        _loginLayer = _homeRoot.Q<VisualElement>("login-layer");
        if (_loginLayer == null)
        {
            Debug.LogError("[HomeUI] login-layer not found in Home.uxml");
            return;
        }

        _btnLogin = _homeRoot.Q<Button>("btn-login");
        _btnMatch = _homeRoot.Q<Button>("btn-match");
        _playerName = _homeRoot.Q<Label>("player-name");
        _netStatus = _homeRoot.Q<Label>("net-status");

        if (_btnLogin != null) _btnLogin.clicked += OpenLogin;
        if (_btnMatch != null) _btnMatch.clicked += OnMatchClicked;

        RefreshPlayerLabel();
    }

    private void UnbindHomeEvents()
    {
        if (_btnLogin != null)
        {
            _btnLogin.clicked -= OpenLogin;
            _btnLogin = null;
        }
        if (_btnMatch != null)
        {
            _btnMatch.clicked -= OnMatchClicked;
            _btnMatch = null;
        }
    }

    private void OpenLogin()
    {
        if (_loginLayer == null || _loginPopup != null) return;
        if (loginUxml == null)
        {
            Debug.LogError("[HomeUI] Assign Login.uxml to loginUxml");
            return;
        }

        var instance = loginUxml.Instantiate();
        _loginPopup = instance;
        _loginLayer.Add(_loginPopup);
        BindLoginEvents(_loginPopup);
    }

    private void BindLoginEvents(VisualElement popup)
    {
        var close = popup.Q<Button>("btn-close");
        var submit = popup.Q<Button>("btn-submit");
        if (close != null) close.clicked += CloseLogin;
        if (submit != null) submit.clicked += SubmitLogin;
    }

    private void UnbindLoginEvents()
    {
        if (_loginPopup == null) return;
        var close = _loginPopup.Q<Button>("btn-close");
        var submit = _loginPopup.Q<Button>("btn-submit");
        if (close != null) close.clicked -= CloseLogin;
        if (submit != null) submit.clicked -= SubmitLogin;
    }

    private void CloseLogin()
    {
        if (_loginPopup == null) return;
        UnbindLoginEvents();
        _loginPopup.RemoveFromHierarchy();
        _loginPopup = null;
    }

    private void SubmitLogin()
    {
        if (_loginPopup == null) return;

        var userField = _loginPopup.Q<TextField>("input-username");
        var passField = _loginPopup.Q<TextField>("input-password");
        var user = userField != null ? userField.value : string.Empty;
        var pass = passField != null ? passField.value : string.Empty;

        if (string.IsNullOrWhiteSpace(user))
            user = "Player";

        if (_session != null)
            _session.PlayerName = user.Trim();

        Debug.Log($"[HomeUI] Login user={user} pass={pass}");
        _loginPub?.Publish(new LoginSuccessMessage(_session?.PlayerName ?? user));

        RefreshPlayerLabel();
        CloseLogin();
    }

    private void OnMatchClicked()
    {
        if (_netStatus != null)
            _netStatus.text = "匹配中...";

        _matchPub?.Publish(new MatchStartMessage(
            GameConstants.DefaultHost,
            GameConstants.DefaultPort));
    }

    private void OnNetState(NetworkStateMessage msg)
    {
        if (_netStatus == null) return;
        _netStatus.text = msg.Connected ? $"已连接 {msg.Detail}" : $"断开: {msg.Detail}";
        _netStatus.style.color = msg.Connected
            ? new StyleColor(Color.green)
            : new StyleColor(new Color(0.55f, 0.55f, 0.58f));
    }

    private void RefreshPlayerLabel()
    {
        if (_playerName == null) return;
        var name = _session != null ? _session.PlayerName : "Guest";
        _playerName.text = $"玩家: {name}";
    }
}
