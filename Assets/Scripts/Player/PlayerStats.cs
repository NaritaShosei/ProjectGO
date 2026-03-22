using System;
using UnityEngine;

public class PlayerStats
{
    // ---- HP ----
    public float InitialMaxHealth { get; private set; }
    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;

    // ---- 雷ゲージ ----
    public float InitialMaxThunderGauge { get; private set; }
    public float MaxThunderGauge => _maxThunderGauge;
    public float CurrentThunderGauge => _currentThunderGauge;
    public float DrainPerSecond => _drainPerSecond;
    public float RecoverPerSecond => _recoverPerSecond;

    /// <summary> 1以上あれば雷神モードを使用可能 </summary>
    public bool CanUseThunder => _currentThunderGauge > 1f;

    // ---- 戦闘ステータス ----
    public float AttackPower => _attackPower;
    public float CriticalRate => _criticalRate;
    public float DefensePower => _defensePower;

    // ---- イベント ----
    public event Action OnDead;
    public event Action<float, float, float> OnHealthChanged;
    /// <summary> (current, max, initialMax) — スタミナと同じ形式 </summary>
    public event Action<float, float, float> OnThunderGaugeChanged;
    public event Action OnThunderGaugeDepleted;
    public event Action OnStatsChanged;

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
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, InitialMaxHealth);
        if (_currentHealth <= 0) OnDead?.Invoke();
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, InitialMaxHealth);
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
            _currentThunderGauge = Mathf.Max(0f, _currentThunderGauge - _drainPerSecond * deltaTime);

            // 枯渇した瞬間だけ OnDepleted を発火
            if (before > 0f && _currentThunderGauge <= 0f)
            {
                OnThunderGaugeChanged?.Invoke(_currentThunderGauge, _maxThunderGauge, InitialMaxThunderGauge);
                OnThunderGaugeDepleted?.Invoke();
                return;
            }
        }
        else
        {
            _currentThunderGauge = Mathf.Min(_maxThunderGauge, _currentThunderGauge + _recoverPerSecond * deltaTime);
        }

        // 変化があったときのみ通知（毎フレーム発火によるUI負荷を抑える）
        if (!Mathf.Approximately(before, _currentThunderGauge))
        {
            OnThunderGaugeChanged?.Invoke(_currentThunderGauge, _maxThunderGauge, InitialMaxThunderGauge);
        }
    }

    // ---- スキルから呼ぶ口 ----

    public void AddMaxThunderGauge(float value)
    {
        if (value <= 0f) return;
        _maxThunderGauge += value;
        OnThunderGaugeChanged?.Invoke(_currentThunderGauge, _maxThunderGauge, InitialMaxThunderGauge);
        OnStatsChanged?.Invoke();
    }

    /// <summary> 消費速度を変更する。負の値で軽減。0未満にはならない。 </summary>
    public void AddDrainPerSecond(float delta)
    {
        _drainPerSecond = Mathf.Max(0f, _drainPerSecond + delta);
        OnStatsChanged?.Invoke();
    }

    /// <summary> 回復速度を変更する。正の値で強化。0未満にはならない。 </summary>
    public void AddRecoverPerSecond(float delta)
    {
        _recoverPerSecond = Mathf.Max(0f, _recoverPerSecond + delta);
        OnStatsChanged?.Invoke();
    }

    // ---- 戦闘ステータス操作 ----

    public void AddAttackPower(float value)
    {
        _attackPower = Mathf.Max(0f, _attackPower + value);
        OnStatsChanged?.Invoke();
    }

    public void AddCriticalRate(float value)
    {
        _criticalRate = Mathf.Max(0f, _criticalRate + value);
        OnStatsChanged?.Invoke();
    }

    public void AddDefensePower(float value)
    {
        _defensePower = Mathf.Max(0f, _defensePower + value);
        OnStatsChanged?.Invoke();
    }

    public void AddMaxHealth(float value)
    {
        if (value <= 0f) return;
        _maxHealth += value;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, InitialMaxHealth);
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
}
