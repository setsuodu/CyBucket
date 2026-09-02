using System.Threading;
using MessagePipe;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// MessagePipe 纯净启动（不依赖 VContainer）
/// 使用内置 BuiltinContainerBuilder
///
/// 依赖：
///   OpenUPM / Git: com.cysharp.messagepipe
///   OpenUPM: com.cysharp.unitask
/// </summary>
public class MessagePipeBootstrap : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool enableStackTrace = false;

    private void Awake()
    {
        var builder = new BuiltinContainerBuilder();

        builder.AddMessagePipe(opt =>
        {
            opt.EnableCaptureStackTrace = enableStackTrace;
        });

        builder.AddMessageBroker<PlayerDamagedMessage>();
        builder.AddMessageBroker<PlayerDiedMessage>();
        builder.AddMessageBroker<ScoreChangedMessage>();

        builder.AddAsyncRequestHandler<GetPlayerStatusRequest, GetPlayerStatusResponse, PlayerStatusRequestHandler>();

        var provider = builder.BuildServiceProvider();
        GlobalMessagePipe.SetProvider(provider);

        Debug.Log("[MessagePipe] GlobalMessagePipe 已设置（BuiltinContainer）");
        DontDestroyOnLoad(gameObject);
    }
}

/// <summary>
/// 异步 Request Handler（Unity 版返回 UniTask）
/// </summary>
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
