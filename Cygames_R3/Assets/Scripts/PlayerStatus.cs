using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private Text comboText;
    [SerializeField] private Text warningText;
    [SerializeField] private Text cooldownText;

    // 响应式状态：修改即自动通知订阅者
    public readonly ReactiveProperty<int> Hp = new(100);
    public readonly ReactiveProperty<int> Combo = new(0);

    private const int MaxHp = 100;

    private void Start()
    {
        // 1. 血量 -> UI 血条，自动绑定，并在销毁时自动取消订阅
        Hp.Subscribe(hp => hpBar.value = (float)hp / MaxHp)
          .AddTo(this); // MonoBehaviour 销毁时自动 Dispose

        // 2. 血量低于 30% 时警告，但用 Throttle 防止连续受伤刷屏
        //    (300ms 内多次触发只取最后一次)
        Hp.Where(hp => hp <= MaxHp * 0.3f && hp > 0)
          .ThrottleLast(TimeSpan.FromMilliseconds(300))
          .Subscribe(_ => ShowWarning("生命值过低！"))
          .AddTo(this);

        // 3. 死亡判定
        Hp.Where(hp => hp <= 0)
          .Take(1) // 只触发一次
          .Subscribe(_ => OnPlayerDeath())
          .AddTo(this);

        // 4. 连击数变化 -> UI，同时 2 秒无新连击则自动清零
        Combo.Subscribe(c => comboText.text = c > 0 ? $"Combo x{c}" : "")
             .AddTo(this);

        Combo.Where(c => c > 0)
             .Debounce(TimeSpan.FromSeconds(2)) // 停止输入2秒后触发
             .Subscribe(_ => Combo.Value = 0)
             .AddTo(this);
    }

    public void TakeDamage(int amount)
    {
        Hp.Value = Mathf.Max(0, Hp.Value - amount);
    }

    public void AddCombo()
    {
        Combo.Value++;
    }

    private void ShowWarning(string msg)
    {
        warningText.text = msg;
    }

    private void OnPlayerDeath()
    {
        Debug.Log("玩家死亡，触发死亡流程");
    }

    // 5. 技能冷却倒计时（Interval 定时流）
    public IDisposable StartCooldown(float seconds)
    {
        float remaining = seconds;
        cooldownText.gameObject.SetActive(true);

        return Observable.Interval(TimeSpan.FromSeconds(0.1f))
            .TakeWhile(_ => remaining > 0)
            .Subscribe(_ =>
            {
                remaining -= 0.1f;
                cooldownText.text = remaining.ToString("F1");
            }, onCompleted: _ =>
            {
                cooldownText.gameObject.SetActive(false);
            });
    }
}