using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class MessagePipeDemo : MonoBehaviour
{
    [Header("UI（可选）")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text logText;
    [SerializeField] private Button damageButton;
    [SerializeField] private Button killButton;
    [SerializeField] private Button scoreButton;
    [SerializeField] private Button requestButton;

    private int _hp = 100;
    private int _score = 0;
    private float _startTime;
    private bool _isDead;

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

        _damagePub = GlobalMessagePipe.GetPublisher<PlayerDamagedMessage>();
        _diedPub = GlobalMessagePipe.GetPublisher<PlayerDiedMessage>();
        _scorePub = GlobalMessagePipe.GetPublisher<ScoreChangedMessage>();

        _damageSub = GlobalMessagePipe.GetSubscriber<PlayerDamagedMessage>();
        _diedSub = GlobalMessagePipe.GetSubscriber<PlayerDiedMessage>();
        _scoreSub = GlobalMessagePipe.GetSubscriber<ScoreChangedMessage>();

        _statusRequester = GlobalMessagePipe.GetAsyncRequestHandler<GetPlayerStatusRequest, GetPlayerStatusResponse>();

        var d1 = _damageSub.Subscribe(OnPlayerDamaged);
        var d2 = _diedSub.Subscribe(OnPlayerDied);
        var d3 = _scoreSub.Subscribe(OnScoreChanged);
        _bag = DisposableBag.Create(d1, d2, d3);

        if (damageButton) damageButton.onClick.AddListener(() => PublishDamage(15));
        if (killButton) killButton.onClick.AddListener(PublishDeath);
        if (scoreButton) scoreButton.onClick.AddListener(() => PublishScore(10));
        if (requestButton) requestButton.onClick.AddListener(() => DoRequestStatus().Forget());

        SyncHandlerState();
        RefreshUI();
        AppendLog("MessagePipe Demo 启动。D/K/S/R 测试");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D)) PublishDamage(UnityEngine.Random.Range(8, 25));
        if (Input.GetKeyDown(KeyCode.K)) PublishDeath();
        if (Input.GetKeyDown(KeyCode.S)) PublishScore(UnityEngine.Random.Range(5, 20));
        if (Input.GetKeyDown(KeyCode.R)) DoRequestStatus().Forget();
    }

    private void SyncHandlerState()
    {
        PlayerStatusRequestHandler.CurrentHp = _hp;
        PlayerStatusRequestHandler.CurrentScore = _score;
        PlayerStatusRequestHandler.IsAlive = !_isDead;
    }

    private void PublishDamage(int amount)
    {
        if (_isDead) return;
        _hp = Mathf.Max(0, _hp - amount);
        SyncHandlerState();
        _damagePub.Publish(new PlayerDamagedMessage(amount, transform.position, "Demo"));
        if (_hp <= 0) PublishDeath();
    }

    private void PublishDeath()
    {
        if (_isDead) return;
        _isDead = true;
        _hp = 0;
        SyncHandlerState();
        _diedPub.Publish(new PlayerDiedMessage("HP depleted", Time.time - _startTime));
    }

    private void PublishScore(int delta)
    {
        if (_isDead) return;
        int old = _score;
        _score += delta;
        SyncHandlerState();
        _scorePub.Publish(new ScoreChangedMessage(old, _score));
    }

    private void OnPlayerDamaged(PlayerDamagedMessage msg)
    {
        AppendLog($"[Damaged] -{msg.Damage} from {msg.Source}");
        RefreshUI();
    }

    private void OnPlayerDied(PlayerDiedMessage msg)
    {
        AppendLog($"[Died] {msg.Reason}, survived {msg.SurvivalTime:F1}s");
        RefreshUI();
    }

    private void OnScoreChanged(ScoreChangedMessage msg)
    {
        AppendLog($"[Score] {msg.OldScore} → {msg.NewScore} ({msg.Delta:+#;-#;0})");
        RefreshUI();
    }

    private async UniTaskVoid DoRequestStatus()
    {
        AppendLog("[Request] 请求中...");
        try
        {
            var res = await _statusRequester.InvokeAsync(
                new GetPlayerStatusRequest(1),
                this.GetCancellationTokenOnDestroy());
            AppendLog($"[Response] HP={res.Hp}, Score={res.Score}, Alive={res.IsAlive}");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[Response] 已取消");
        }
        catch (Exception e)
        {
            AppendLog($"[Response] 错误: {e.Message}");
        }
    }

    private void RefreshUI()
    {
        if (statusText == null) return;
        statusText.text = $"HP: {_hp}\nScore: {_score}\n{(_isDead ? "DEAD" : "ALIVE")}\nTime: {Time.time - _startTime:F1}s";
    }

    private void AppendLog(string msg)
    {
        Debug.Log($"[MessagePipe] {msg}");
        if (logText == null) return;
        logText.text = $"{msg}\n{logText.text}";
        if (logText.text.Length > 800)
            logText.text = logText.text.Substring(0, 600);
    }

    private void OnDestroy() => _bag?.Dispose();
}