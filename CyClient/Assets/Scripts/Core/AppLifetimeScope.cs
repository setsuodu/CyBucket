using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 全局 DI 根。场景挂一个物体。
/// UI 由 PanelRenderer + HomeUI 驱动，不再注册 UIService。
/// </summary>
public class AppLifetimeScope : LifetimeScope
{
    [SerializeField] private bool enableMessagePipeStackTrace;

    protected override void Configure(IContainerBuilder builder)
    {
        var mpOptions = builder.RegisterMessagePipe(o =>
        {
            o.EnableCaptureStackTrace = enableMessagePipeStackTrace;
        });
        builder.RegisterMessageBroker<LoginSuccessMessage>(mpOptions);
        builder.RegisterMessageBroker<MatchStartMessage>(mpOptions);
        builder.RegisterMessageBroker<PlayerSpawnedMessage>(mpOptions);
        builder.RegisterMessageBroker<NetworkStateMessage>(mpOptions);

        builder.RegisterBuildCallback(c =>
        {
            GlobalMessagePipe.SetProvider(c.AsServiceProvider());
        });

        builder.Register<GameSession>(Lifetime.Singleton);
        builder.Register<NetworkService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<AppEntryPoint>();
    }
}
