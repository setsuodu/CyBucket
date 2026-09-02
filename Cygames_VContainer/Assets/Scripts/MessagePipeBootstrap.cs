using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using System.Threading;

public class MessagePipeBootstrap : LifetimeScope
{
    [SerializeField] private bool enableStackTrace = false;

    protected override void Configure(IContainerBuilder builder)
    {
        var options = builder.RegisterMessagePipe(opt =>
        {
            opt.EnableCaptureStackTrace = enableStackTrace;
        });

        builder.RegisterMessageBroker<PlayerDamagedMessage>(options);
        builder.RegisterMessageBroker<PlayerDiedMessage>(options);
        builder.RegisterMessageBroker<ScoreChangedMessage>(options);

        builder.RegisterAsyncRequestHandler<GetPlayerStatusRequest, GetPlayerStatusResponse, PlayerStatusRequestHandler>(options);

        builder.RegisterBuildCallback(container =>
        {
            GlobalMessagePipe.SetProvider(container.AsServiceProvider());
            Debug.Log("[MessagePipe] GlobalMessagePipe ready (VContainer + OpenUPM)");
        });
    }
}

public class PlayerStatusRequestHandler : IAsyncRequestHandler<GetPlayerStatusRequest, GetPlayerStatusResponse>
{
    public static int CurrentHp = 100;
    public static int CurrentScore = 0;
    public static bool IsAlive = true;

    public async UniTask<GetPlayerStatusResponse> InvokeAsync(
        GetPlayerStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        await UniTask.Delay(20, cancellationToken: cancellationToken);
        return new GetPlayerStatusResponse(CurrentHp, CurrentScore, IsAlive);
    }
}