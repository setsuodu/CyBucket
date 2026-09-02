using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// PanelRenderer 入口。Login 是独立 UXML，运行时 Instantiate 到 #login-layer。
/// 居中用面板像素计算，不靠 flex / translate 百分比。
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public sealed class HomeUI : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset loginUxml;

    private PanelRenderer _panelRenderer;
    private VisualElement _homeRoot;
    private VisualElement _loginLayer;
    private VisualElement _loginRoot; // Instantiate 出来的整棵（TemplateContainer）

    private Button _btnLogin;
    private Button _btnMatch;
    private Label _playerName;
    private Label _netStatus;

    private GameSession _session;
    private IPublisher<MatchStartMessage> _matchPub;
    private IPublisher<LoginSuccessMessage> _loginPub;
    private ISubscriber<NetworkStateMessage> _netSub;
    private IDisposable _netBag;

    private void Awake()
    {
        _panelRenderer = GetComponent<PanelRenderer>();
        _panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void Start()
    {
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
        _loginRoot = null;
        _loginLayer = null;
        _homeRoot = null;
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        UnbindHomeEvents();
        UnbindLoginEvents();
        _loginRoot = null;

        _homeRoot = root;
        _loginLayer = _homeRoot.Q<VisualElement>("login-layer");
        if (_loginLayer == null)
        {
            Debug.LogError("[HomeUI] #login-layer missing in Home.uxml");
            return;
        }

        // layer 强制铺满 Home，否则子节点百分比/居中全废
        _loginLayer.style.position = Position.Absolute;
        _loginLayer.style.left = 0;
        _loginLayer.style.right = 0;
        _loginLayer.style.top = 0;
        _loginLayer.style.bottom = 0;
        _loginLayer.style.width = Length.Percent(100);
        _loginLayer.style.height = Length.Percent(100);

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
        if (_btnLogin != null) { _btnLogin.clicked -= OpenLogin; _btnLogin = null; }
        if (_btnMatch != null) { _btnMatch.clicked -= OnMatchClicked; _btnMatch = null; }
    }

    // -------------------------------------------------------------------------
    // Login：独立 UXML，不是把所有功能塞成一层子物体业务树
    // -------------------------------------------------------------------------
    private void OpenLogin()
    {
        if (_loginLayer == null || _loginRoot != null) return;
        if (loginUxml == null)
        {
            Debug.LogError("[HomeUI] 把 Login.uxml 拖到 HomeUI.loginUxml");
            return;
        }

        // Instantiate 返回 TemplateContainer，里面才是 #login-popup
        var templateRoot = loginUxml.Instantiate();
        _loginRoot = templateRoot;
        _loginLayer.Add(templateRoot);

        // 等一帧布局，再按面板像素居中（避免 resolvedStyle 仍是 NaN）
        templateRoot.schedule.Execute(ApplyLoginLayoutAndBind).ExecuteLater(0);
    }

    private void ApplyLoginLayoutAndBind()
    {
        if (_loginRoot == null || _homeRoot == null) return;

        float panelW = _homeRoot.resolvedStyle.width;
        float panelH = _homeRoot.resolvedStyle.height;
        if (float.IsNaN(panelW) || panelW < 1f) panelW = Screen.width;
        if (float.IsNaN(panelH) || panelH < 1f) panelH = Screen.height;

        // 遮罩铺满
        var popup = _loginRoot.Q<VisualElement>("login-popup") ?? _loginRoot;
        popup.style.position = Position.Absolute;
        popup.style.left = 0;
        popup.style.top = 0;
        popup.style.right = StyleKeyword.Auto;
        popup.style.bottom = StyleKeyword.Auto;
        popup.style.width = panelW;
        popup.style.height = panelH;

        var dim = _loginRoot.Q<VisualElement>("login-dim");
        if (dim != null)
        {
            dim.style.position = Position.Absolute;
            dim.style.left = 0;
            dim.style.top = 0;
            dim.style.width = panelW;
            dim.style.height = panelH;
        }

        // 登录框：固定尺寸，像素居中（这里的 loginBox 就是 UXML 里的 #login-window）
        var loginBox = _loginRoot.Q<VisualElement>("login-window");
        if (loginBox == null)
        {
            Debug.LogError("[HomeUI] #login-window not found in Login.uxml");
            return;
        }

        const float boxW = 420f;
        const float boxH = 300f;
        float left = (panelW - boxW) * 0.5f;
        float top = (panelH - boxH) * 0.5f;

        loginBox.style.position = Position.Absolute;
        loginBox.style.left = left;
        loginBox.style.top = top;
        loginBox.style.width = boxW;
        loginBox.style.height = boxH;
        loginBox.style.right = StyleKeyword.Auto;
        loginBox.style.bottom = StyleKeyword.Auto;
        // 清掉可能把布局搞飞的 translate
        loginBox.style.translate = new Translate(0, 0, 0);

        BindLoginEvents(_loginRoot);
        Debug.Log($"[HomeUI] Login box panel={panelW}x{panelH} pos=({left},{top})");
    }

    private void BindLoginEvents(VisualElement root)
    {
        var close = root.Q<Button>("btn-close");
        var submit = root.Q<Button>("btn-submit");
        if (close != null) close.clicked += CloseLogin;
        if (submit != null) submit.clicked += SubmitLogin;
    }

    private void UnbindLoginEvents()
    {
        if (_loginRoot == null) return;
        var close = _loginRoot.Q<Button>("btn-close");
        var submit = _loginRoot.Q<Button>("btn-submit");
        if (close != null) close.clicked -= CloseLogin;
        if (submit != null) submit.clicked -= SubmitLogin;
    }

    private void CloseLogin()
    {
        if (_loginRoot == null) return;
        UnbindLoginEvents();
        _loginRoot.RemoveFromHierarchy();
        _loginRoot = null;
    }

    private void SubmitLogin()
    {
        if (_loginRoot == null) return;
        var userField = _loginRoot.Q<TextField>("input-username");
        var passField = _loginRoot.Q<TextField>("input-password");
        var user = userField != null ? userField.value : "";
        var pass = passField != null ? passField.value : "";
        if (string.IsNullOrWhiteSpace(user)) user = "Player";

        if (_session != null) _session.PlayerName = user.Trim();
        Debug.Log($"[HomeUI] Login user={user}");
        _loginPub?.Publish(new LoginSuccessMessage(_session?.PlayerName ?? user));
        RefreshPlayerLabel();
        CloseLogin();
    }

    private void OnMatchClicked()
    {
        if (_netStatus != null) _netStatus.text = "匹配中...";
        _matchPub?.Publish(new MatchStartMessage(GameConstants.DefaultHost, GameConstants.DefaultPort));
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
        _playerName.text = $"玩家: {(_session != null ? _session.PlayerName : "Guest")}";
    }
}
