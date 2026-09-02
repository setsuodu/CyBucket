using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 启动流程：AB 初始化 → Login UI
/// ABManager 来自 com.setsuodu.assetbundleframework
/// </summary>
public class AppEntryPoint : IAsyncStartable
{
    private readonly UIService _ui;

    public AppEntryPoint(UIService ui) => _ui = ui;

    public async UniTask StartAsync(CancellationToken ct)
    {
        Debug.Log("[App] Boot...");
        try
        {
            await ABManager.Instance.InitializeAsync(ct);
            Debug.Log("[App] ABManager ready");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[App] AB init: {e.Message} (Editor 可用占位预制体)");
        }

        await _ui.ShowLoginAsync(ct);
    }
}
