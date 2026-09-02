using MessagePipe;
using UnityEngine.UIElements;
using VContainer;

public class LoginPresenter
{
    private readonly UIDocument _doc;
    private readonly GameSession _session;
    private readonly UIService _ui;
    private readonly IPublisher<LoginSuccessMessage> _loginPub;
    private VisualElement _root;

    public LoginPresenter(UIDocument doc, IObjectResolver resolver)
    {
        _doc = doc;
        _session = resolver.Resolve<GameSession>();
        _ui = resolver.Resolve<UIService>();
        _loginPub = resolver.Resolve<IPublisher<LoginSuccessMessage>>();
    }

    public void Show()
    {
        _doc.visualTreeAsset = null; // 若用独立 uxml 可在此赋值
        _root = _doc.rootVisualElement;
        _root.Clear();

        var box = new VisualElement();
        box.style.flexGrow = 1;
        box.style.justifyContent = Justify.Center;
        box.style.alignItems = Align.Center;
        box.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.1f, 0.1f, 0.15f));

        var title = new Label("CyClient Login");
        title.style.fontSize = 28;
        title.style.color = UnityEngine.Color.white;
        title.style.marginBottom = 20;

        var nameField = new TextField("Name");
        nameField.value = _session.PlayerName;
        nameField.style.width = 280;
        nameField.style.marginBottom = 16;

        var btn = new Button(() => OnLogin(nameField.value)) { text = "进入" };
        btn.style.width = 280;
        btn.style.height = 40;
        btn.style.fontSize = 18;

        box.Add(title);
        box.Add(nameField);
        box.Add(btn);
        _root.Add(box);
    }

    public void Hide() => _root?.Clear();

    private async void OnLogin(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) name = "Player";
        _session.PlayerName = name.Trim();
        _loginPub.Publish(new LoginSuccessMessage(_session.PlayerName));
        await _ui.ShowHomeAsync(default);
    }
}
