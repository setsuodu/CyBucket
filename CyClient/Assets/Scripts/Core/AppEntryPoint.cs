using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 启动：初始化 AB。UI 由场景里 PanelRenderer + HomeUI 自动起来。
/// </summary>
public class AppEntryPoint : IAsyncStartable
{
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
    }
}
