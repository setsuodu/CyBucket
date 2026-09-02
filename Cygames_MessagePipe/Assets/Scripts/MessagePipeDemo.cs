using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MessagePipe 完整示例：
/// - Pub/Sub（广播）
/// - Request-Response（异步请求）
///
/// 快捷键：
///   D - 造成伤害
///   K - 强制死亡
///   S - 加分
///   R - 请求当前状态（Request-Response）
/// </summary>
public class MessagePipeDemo : MonoBehaviour
{
    [Header("UI（可选）")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text logText;
    [SerializeField] private Button damageButton;
    [SerializeField] private Button killButton;
    [SerializeField] private Button scoreButton;
    [SerializeField] private Button requestButton;

    // 本地状态（模拟玩家）
    private int _hp = 100;
    private int _score = 0;
    private float _startTime;
    private bool _isDead;

    // MessagePipe 句柄
    private IPublisher<PlayerDamagedMessage> _damagePub;
    private IPublisher<PlayerDiedMessage> _diedPub;
    private IPublisher<ScoreChangedMessage> _scorePub;

    private ISubscriber<PlayerDamagedMessage> _damageSub;
    private ISubscriber<PlayerDiedMessage> _diedSub;
    private ISubscriber<ScoreChangedMessage> _scoreSub;

    private IAsyncRequestHandler<GetPlayerStatusRequest, GetPlayerStatusResponse> _statusRequester;

    private IDisposable _bag;

    private void Start()
    {
        _startTime = Time.time;

        // 确保 Bootstrap 已执行（GlobalMessagePipe 可用）
        if (!IsGlobalReady())
        {
            Debug.LogError("[MessagePipeDemo] GlobalMessagePipe 未初始化，请先挂 MessagePipeBootstrap！");
            enabled = false;
            return;
        }

        // ========== 获取 Publisher / Subscriber ==========
        _damagePub = GlobalMessagePipe.GetPublisher<PlayerDamagedMessage>();
        _diedPub   = GlobalMessagePipe.GetPublisher<PlayerDiedMessage>();
        _scorePub  = GlobalMessagePipe.GetPublisher<ScoreChangedMessage>();

        _damageSub = GlobalMessagePipe.GetSubscriber<PlayerDamagedMessage>();
        _diedSub   = GlobalMessagePipe.GetSubscriber<PlayerDiedMessage>();
        _scoreSub  = GlobalMessagePipe.GetSubscriber<ScoreChangedMessage>();

        // Request-Response
        _statusRequester = GlobalMessagePipe.GetAsyncRequestHandler<GetPlayerStatusRequest, GetPlayerStatusResponse>();

        // ========== 订阅 ==========
        var d1 = _damageSub.Subscribe(OnPlayerDamaged);
        var d2 = _diedSub.Subscribe(OnPlayerDied);
        var d3 = _scoreSub.Subscribe(OnScoreChanged);

        _bag = DisposableBag.Create(d1, d2, d3);

        // 按钮
        if (damageButton) damageButton.onClick.AddListener(() => PublishDamage(15));
        if (killButton)   killButton.onClick.AddListener(PublishDeath);
        if (scoreButton)  scoreButton.onClick.AddListener(() => PublishScore(10));
        if (requestButton) requestButton.onClick.AddListener(DoRequestStatus);

        // 同步初始状态到 Handler
        SyncHandlerState();

        RefreshUI();
        AppendLog("MessagePipe Demo 已启动。按 D/K/S/R 测试。");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D)) PublishDamage(UnityEngine.Random.Range(8, 25));
        if (Input.GetKeyDown(KeyCode.K)) PublishDeath();
        if (Input.GetKeyDown(KeyCode.S)) PublishScore(UnityEngine.Random.Range(5, 20));
        if (Input.GetKeyDown(KeyCode.R)) DoRequestStatus();
    }

    private static bool IsGlobalReady()
    {
        try
        {
            // 尝试获取任意一个，失败说明未 SetProvider
            GlobalMessagePipe.GetPublisher<PlayerDamagedMessage>();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SyncHandlerState()
    {
        PlayerStatusRequestHandler.CurrentHp = _hp;
        PlayerStatusRequestHandler.CurrentScore = _score;
        PlayerStatusRequestHandler.IsAlive = !_isDead;
    }

    // ========== 发布 ==========
    private void PublishDamage(int amount)
    {
        if (_isDead) return;

        _hp = Mathf.Max(0, _hp - amount);
        SyncHandlerState();

        var msg = new PlayerDamagedMessage(amount, transform.position, "Demo");
        _damagePub.Publish(msg);

        if (_hp <= 0)
            PublishDeath();
    }

    private void PublishDeath()
    {
        if (_isDead) return;
        _isDead = true;
        _hp = 0;
        SyncHandlerState();

        var msg = new PlayerDiedMessage("HP depleted", Time.time - _startTime);
        _diedPub.Publish(msg);
    }

    private void PublishScore(int delta)
    {
        if (_isDead) return;
        int old = _score;
        _score += delta;
        SyncHandlerState();
        _scorePub.Publish(new ScoreChangedMessage(old, _score));
    }

    // ========== 订阅回调 ==========
    private void OnPlayerDamaged(PlayerDamagedMessage msg)
    {
        AppendLog($"[Damaged] -{msg.Damage} from {msg.Source} @ {msg.HitPoint}");
        RefreshUI();
    }

    private void OnPlayerDied(PlayerDiedMessage msg)
    {
        AppendLog($"[Died] reason={msg.Reason}, survived={msg.SurvivalTime:F1}s");
        RefreshUI();
    }

    private void OnScoreChanged(ScoreChangedMessage msg)
    {
        AppendLog($"[Score] {msg.OldScore} → {msg.NewScore} ({msg.Delta:+#;-#;0})");
        RefreshUI();
    }

    // ========== Request-Response ==========
    private async void DoRequestStatus()
    {
        AppendLog("[Request] 正在请求玩家状态...");
        try
        {
            var response = await _statusRequester.InvokeAsync(
                new GetPlayerStatusRequest(1),
                destroyCancellationToken);

            AppendLog($"[Response] HP={response.Hp}, Score={response.Score}, Alive={response.IsAlive}");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[Response] 请求被取消");
        }
        catch (Exception e)
        {
            AppendLog($"[Response] 错误: {e.Message}");
        }
    }

    // ========== UI ==========
    private void RefreshUI()
    {
        if (statusText == null) return;
        statusText.text =
            $"HP: {_hp}\n" +
            $"Score: {_score}\n" +
            $"Status: {(_isDead ? "DEAD" : "ALIVE")}\n" +
            $"Time: {Time.time - _startTime:F1}s";
    }

    private void AppendLog(string msg)
    {
        Debug.Log($"[MessagePipe] {msg}");
        if (logText != null)
        {
            logText.text = $"{msg}\n{logText.text}";
            if (logText.text.Length > 800)
                logText.text = logText.text.Substring(0, 600);
        }
    }

    private void OnDestroy()
    {
        _bag?.Dispose();
    }
}
