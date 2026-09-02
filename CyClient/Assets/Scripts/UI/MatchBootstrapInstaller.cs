using MessagePipe;
using UnityEngine;
using VContainer;

/// <summary>
/// 把 MatchBootstrap 与 DI 服务接上（可挂在同场景）
/// </summary>
public class MatchBootstrapInstaller : MonoBehaviour
{
    [SerializeField] private MatchBootstrap bootstrap;

    private void Start()
    {
        var scope = FindObjectOfType<AppLifetimeScope>();
        if (scope == null || bootstrap == null) return;

        var net = scope.Container.Resolve<NetworkService>();
        var session = scope.Container.Resolve<GameSession>();
        var spawnSub = scope.Container.Resolve<ISubscriber<PlayerSpawnedMessage>>();
        var matchSub = scope.Container.Resolve<ISubscriber<MatchStartMessage>>();
        bootstrap.Init(net, session, spawnSub, matchSub);
    }
}
