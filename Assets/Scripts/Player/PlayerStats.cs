using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats
{
    // ---- HP ----
    public float InitialMaxHealth { get; private set; }
    public float MaxHealth =>
        ApplyModifiers(StatType.Health, _maxHealth);
    public float CurrentHealth => _currentHealth;

    // ---- 雷ゲージ ----
    public float InitialMaxThunderGauge { get; private set; }
    public float MaxThunderGauge =>
        ApplyModifiers(StatType.ThunderGauge, _maxThunderGauge);

    public float CurrentThunderGauge => _currentThunderGauge;

    public float DrainPerSecond =>
        ApplyModifiers(StatType.ThunderDrain, _drainPerSecond);

    public float RecoverPerSecond =>
        ApplyModifiers(StatType.ThunderRecover, _recoverPerSecond);

    /// <summary> 1以上あれば雷神モードを使用可能 </summary>
    public bool CanUseThunder => _currentThunderGauge > 1f;

    // ---- 戦闘ステータス ----
    public float AttackPower =>
        ApplyModifiers(StatType.Attack, _attackPower);

    public float CriticalRate =>
        ApplyModifiers(StatType.CriticalRate, _criticalRate);

    public float DefensePower =>
        ApplyModifiers(StatType.Defense, _defensePower);

    // --- 回復量 ----
    public float HealPower =>
        ApplyModifiers(StatType.Heal, 1f);

    // ---- イベント ----
    public event Action OnDead;

    /// <summary>
    /// 死亡直前イベント。true を返すと死亡をキャンセルする。
    /// </summary>
    public event Func<bool> OnBeforeDead;

    public event Action<float, float, float> OnHealthChanged;
    /// <summary> (current, max, initialMax) — スタミナと同じ形式 </summary>
    public event Action<float, float, float> OnThunderGaugeChanged;
    public event Action OnThunderGaugeDepleted;
    public event Action OnStatsChanged;
    public event Action OnJustDodgeSuccess;
    public event Action OnEndDodge;

    public PlayerStats(PlayerData data)
    {
        // HP
        _maxHealth = data.Stats.MaxHealth;
        InitialMaxHealth = _maxHealth;
        _currentHealth = _maxHealth;

        // 雷ゲージ
        _maxThunderGauge = data.Stats.MaxThunderGauge;
        InitialMaxThunderGauge = _maxThunderGauge;
        _currentThunderGauge = _maxThunderGauge;
        _drainPerSecond = data.ThunderDrainPerSecond;
        _recoverPerSecond = data.ThunderRecoverPerSecond;

        // 戦闘
        _attackPower = data.AttackPower;
        _criticalRate = data.CriticalRate;
        _defensePower = data.DefensePower;
    }

    // ---- HP操作 ----

    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth, InitialMaxHealth);

        // HPが0以下なら死亡処理へ（ただしイベントでキャンセルされる可能性もある）
        if (_currentHealth > 0) { return; }

        var beforeDeadHandlers = OnBeforeDead?.GetInvocationList();

        // 死亡直前イベントで死亡が覆される場合は、死亡処理を行わない。
        if (beforeDeadHandlers != null)
        {
            // 死亡直前イベントを発火。true を返すハンドラーがあれば死亡をキャンセル。
            foreach (Func<bool> handler in beforeDeadHandlers)
            {
                // 例外が発生しても他のハンドラーは呼び続けるため、個別に try-catch する。
                try
                {
                    if (handler.Invoke()) return;
                }
                catch (Exception e)
                {
                    Debug.LogError($"エラーが発生しましたが、死亡処理は続行します。例外: {e}");
                }
            }
        }

        // 死亡イベントを発火
        OnDead?.Invoke();
    }

    public void Heal(float amount)
    {
        amount *= HealPower;

        _currentHealth =
            Mathf.Min(MaxHealth, _currentHealth + amount);

        OnHealthChanged?.Invoke(
            _currentHealth,
            MaxHealth,
            InitialMaxHealth);
    }

    // ---- 雷ゲージ操作 ----

    /// <summary>
    /// Player.Update から毎フレーム呼ぶ。
    /// isThunderMode = true のとき消費、false のとき回復。
    /// </summary>
    public void TickThunderGauge(float deltaTime, bool isThunderMode)
    {
        float before = _currentThunderGauge;

        if (isThunderMode)
        {
            _currentThunderGauge = Mathf.Max(0f, _currentThunderGauge - DrainPerSecond * deltaTime);

            // 枯渇した瞬間だけ OnDepleted を発火
            if (before > 0f && _currentThunderGauge <= 0f)
            {
                OnThunderGaugeChanged?.Invoke(_currentThunderGauge, MaxThunderGauge, InitialMaxThunderGauge);
                OnThunderGaugeDepleted?.Invoke();
                return;
            }
        }
        else
        {
            _currentThunderGauge = Mathf.Min(MaxThunderGauge, _currentThunderGauge + RecoverPerSecond * deltaTime);
        }

        // 変化があったときのみ通知（毎フレーム発火によるUI負荷を抑える）
        if (!Mathf.Approximately(before, _currentThunderGauge))
        {
            OnThunderGaugeChanged?.Invoke(_currentThunderGauge, MaxThunderGauge, InitialMaxThunderGauge);
        }
    }

    // --- モディファイア操作 ----
    public void AddModifier(IStatModifier modifier)
    {
        if (!_modifiers.ContainsKey(modifier.TargetStat))
            _modifiers[modifier.TargetStat] = new List<IStatModifier>();

        _modifiers[modifier.TargetStat].Add(modifier);

        ClampCurrentValues();

        NotifyStatChanged(modifier.TargetStat);

        OnStatsChanged?.Invoke();
    }

    // ---- フィールド ----

    private float _maxHealth;
    private float _currentHealth;

    private float _maxThunderGauge;
    private float _currentThunderGauge;
    private float _drainPerSecond;
    private float _recoverPerSecond;

    private float _attackPower;
    private float _criticalRate;
    private float _defensePower;

    private readonly Dictionary<StatType, List<IStatModifier>> _modifiers = new();

    private float ApplyModifiers(
    StatType statType,
    float baseValue)
    {
        if (!_modifiers.TryGetValue(statType, out var mods))
            return baseValue;

        float value = baseValue;

        foreach (var mod in mods)
        {
            value = mod.Modify(value);
        }

        return value;
    }

    private void ClampCurrentValues()
    {
        _currentHealth = Mathf.Min(_currentHealth, MaxHealth);

        _currentThunderGauge =
            Mathf.Min(_currentThunderGauge, MaxThunderGauge);
    }

    private void NotifyStatChanged(StatType statType)
    {
        switch (statType)
        {
            case StatType.Health:
                OnHealthChanged?.Invoke(
                    _currentHealth,
                    MaxHealth,
                    InitialMaxHealth);
                break;

            case StatType.ThunderGauge:
                OnThunderGaugeChanged?.Invoke(
                    _currentThunderGauge,
                    MaxThunderGauge,
                    InitialMaxThunderGauge);
                break;
        }
    }
}
