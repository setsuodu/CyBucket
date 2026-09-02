using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public class AppEntryPoint : IAsyncStartable
{
    public async UniTask StartAsync(CancellationToken ct)
    {
        Debug.Log("[App] Boot...");
        try
        {
            await ResManager.InitializeAsync(ct);
            Debug.Log("[App] ResManager ready");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[App] Res init: {e.Message}");
        }
    }
}
