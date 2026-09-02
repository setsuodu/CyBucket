using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// UI Toolkit 面板切换（Login / Home）
/// 场景里放一个 UIDocument，挂到此服务。
/// </summary>
public class UIService
{
    private readonly IObjectResolver _resolver;
    private UIDocument _doc;
    private LoginPresenter _login;
    private HomePresenter _home;

    public UIService(IObjectResolver resolver)
    {
        _resolver = resolver;
    }

    public void BindDocument(UIDocument doc)
    {
        _doc = doc;
    }

    public async UniTask ShowLoginAsync(CancellationToken ct)
    {
        EnsureDoc();
        _home?.Hide();
        if (_login == null)
            _login = new LoginPresenter(_doc, _resolver);
        _login.Show();
        await UniTask.Yield(ct);
    }

    public async UniTask ShowHomeAsync(CancellationToken ct)
    {
        EnsureDoc();
        _login?.Hide();
        if (_home == null)
            _home = new HomePresenter(_doc, _resolver);
        _home.Show();
        await UniTask.Yield(ct);
    }

    private void EnsureDoc()
    {
        if (_doc != null) return;
        _doc = Object.FindObjectOfType<UIDocument>();
        if (_doc == null)
            Debug.LogError("[UI] 场景中缺少 UIDocument");
    }
}
