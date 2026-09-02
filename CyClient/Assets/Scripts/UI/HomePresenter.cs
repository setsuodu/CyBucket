using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class HomePresenter
{
    private readonly UIDocument _doc;
    private readonly GameSession _session;
    private readonly IPublisher<MatchStartMessage> _matchPub;
    private readonly ISubscriber<NetworkStateMessage> _netSub;
    private VisualElement _root;
    private Label _status;
    private IDisposable _sub;

    public HomePresenter(UIDocument doc, IObjectResolver resolver)
    {
        _doc = doc;
        _session = resolver.Resolve<GameSession>();
        _matchPub = resolver.Resolve<IPublisher<MatchStartMessage>>();
        _netSub = resolver.Resolve<ISubscriber<NetworkStateMessage>>();
    }

    public void Show()
    {
        _root = _doc.rootVisualElement;
        _root.Clear();

        var box = new VisualElement();
        box.style.flexGrow = 1;
        box.style.justifyContent = Justify.Center;
        box.style.alignItems = Align.Center;
        box.style.backgroundColor = new StyleColor(new Color(0.12f, 0.14f, 0.18f));

        var hi = new Label($"你好, {_session.PlayerName}");
        hi.style.fontSize = 24;
        hi.style.color = Color.white;
        hi.style.marginBottom = 12;

        _status = new Label("未连接");
        _status.style.color = Color.gray;
        _status.style.marginBottom = 24;

        var matchBtn = new Button(OnMatch) { text = "匹配" };
        matchBtn.style.width = 280;
        matchBtn.style.height = 48;
        matchBtn.style.fontSize = 20;

        var tip = new Label($"服务器 {GameConstants.DefaultHost}:{GameConstants.DefaultPort}\n请先启动 MinimalGameServer");
        tip.style.unityTextAlign = TextAnchor.MiddleCenter;
        tip.style.color = Color.gray;
        tip.style.marginTop = 16;

        box.Add(hi);
        box.Add(_status);
        box.Add(matchBtn);
        box.Add(tip);
        _root.Add(box);

        _sub?.Dispose();
        _sub = _netSub.Subscribe(m =>
        {
            _status.text = m.Connected ? $"已连接 {m.Detail}" : $"断开: {m.Detail}";
            _status.style.color = m.Connected ? Color.green : Color.gray;
        });
    }

    public void Hide()
    {
        _sub?.Dispose();
        _root?.Clear();
    }

    private void OnMatch()
    {
        _status.text = "匹配中...";
        _matchPub.Publish(new MatchStartMessage(GameConstants.DefaultHost, GameConstants.DefaultPort));
    }
}
