using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private Text comboText;
    [SerializeField] private Text warningText;
    [SerializeField] private Text cooldownText;
    [SerializeField] private Text statusText;

    [Header("Test Buttons (可选)")]
    [SerializeField] private Button damageButton;
    [SerializeField] private Button comboButton;
    [SerializeField] private Button skillButton;

    // ========== 响应式状态 ==========
    public readonly ReactiveProperty<int> Hp = new(100);
    public readonly ReactiveProperty<int> Combo = new(0);
    public readonly ReactiveProperty<bool> IsDead = new(false);

    // 只读对外暴露（推荐写法）
    public ReadOnlyReactiveProperty<int> HpReadonly => Hp;
    public ReadOnlyReactiveProperty<int> ComboReadonly => Combo;

    private const int MaxHp = 100;
    private IDisposable _cooldownDisposable;

    private void Start()
    {
        // 1. 血量绑定 UI
        Hp.Subscribe(hp =>
        {
            if (hpBar != null) hpBar.value = (float)hp / MaxHp;
        }).AddTo(this);

        // 2. 低血量警告（ThrottleLast 防刷）
        Hp.Where(hp => hp <= MaxHp * 0.3f && hp > 0)
          .ThrottleLast(TimeSpan.FromMilliseconds(300))
          .Subscribe(_ => ShowWarning("生命值过低！"))
          .AddTo(this);

        // 3. 死亡判定（只触发一次）
        Hp.Where(hp => hp <= 0)
          .Take(1)
          .Subscribe(_ =>
          {
              IsDead.Value = true;
              OnPlayerDeath();
          })
          .AddTo(this);

        // 4. 连击 UI + Debounce 自动清零
        Combo.Subscribe(c =>
        {
            if (comboText != null)
                comboText.text = c > 0 ? $"Combo x{c}" : "";
        }).AddTo(this);

        Combo.Where(c => c > 0)
             .Debounce(TimeSpan.FromSeconds(2))
             .Subscribe(_ => Combo.Value = 0)
             .AddTo(this);

        // 5. CombineLatest：把 Hp + Combo 合成一个状态显示
        Observable.CombineLatest(Hp, Combo, (hp, combo) => (hp, combo))
            .Subscribe(t =>
            {
                if (statusText != null)
                    statusText.text = $"HP:{t.hp}  Combo:{t.combo}";
            })
            .AddTo(this);

        // 6. 每帧检测（Frame 特性）
        Observable.EveryUpdate()
            .Where(_ => Input.GetKeyDown(KeyCode.Space))
            .Subscribe(_ => TakeDamage(10))
            .AddTo(this);

        // 7. 按钮绑定（如果有）
        if (damageButton != null)
            damageButton.OnClickAsObservable()
                .Subscribe(_ => TakeDamage(15))
                .AddTo(this);

        if (comboButton != null)
            comboButton.OnClickAsObservable()
                .Subscribe(_ => AddCombo())
                .AddTo(this);

        if (skillButton != null)
            skillButton.OnClickAsObservable()
                .Subscribe(_ => StartCooldown(5f))
                .AddTo(this);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead.Value) return;
        Hp.Value = Mathf.Max(0, Hp.Value - amount);
    }

    public void AddCombo()
    {
        if (IsDead.Value) return;
        Combo.Value++;
    }

    public void StartCooldown(float seconds)
    {
        // 取消上一次冷却
        _cooldownDisposable?.Dispose();

        // 冷却开始：禁用按钮
        if (skillButton != null)
            skillButton.interactable = false;

        float remaining = seconds;
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = remaining.ToString("F1");
        }

        _cooldownDisposable = Observable.Interval(TimeSpan.FromSeconds(0.1f))
            .TakeWhile(_ => remaining > 0)
            .Subscribe(
                onNext: _ =>
                {
                    remaining -= 0.1f;
                    if (cooldownText != null)
                        cooldownText.text = Mathf.Max(0, remaining).ToString("F1");
                },
                onCompleted: _ =>
                {
                    // 冷却结束：恢复按钮
                    if (skillButton != null)
                        skillButton.interactable = true;

                    if (cooldownText != null)
                        cooldownText.gameObject.SetActive(false);
                })
            .AddTo(this);
    }

    private void ShowWarning(string msg)
    {
        if (warningText != null)
            warningText.text = msg;
        Debug.Log($"[Warning] {msg}");
    }

    private void OnPlayerDeath()
    {
        Debug.Log("玩家死亡，触发死亡流程");
        if (warningText != null)
            warningText.text = "你已死亡";
    }

    private void OnDestroy()
    {
        _cooldownDisposable?.Dispose();
    }
}