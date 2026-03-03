using System;
using UnityEngine;

public class Player : MonoBehaviour, IPlayer, IStamina, ISpeedChange
{
    public float AttackPower => _playerStats.AttackPower;
    public float CriticalRate => _playerStats.CriticalRate;

    public float DefensePower => _playerStats.DefensePower;

    public float MaxHealth => _playerStats.MaxHealth;

    public float CurrentHealth => _playerStats.CurrentHealth;

    public float MaxStamina => _playerStats.MaxStamina;

    public float CurrentStamina => _playerStats.CurrentStamina;

    public float TimeScale { get; set; } = 1f;

    public event Action OnDead;
    public event Action<float, float> OnHealthChanged
    {
        add => _playerStats.OnHealthChanged += value;
        remove => _playerStats.OnHealthChanged -= value;
    }

    public event Action<float, float> OnStaminaChanged
    {
        add => _playerStats.OnStaminaChanged += value;
        remove => _playerStats.OnStaminaChanged -= value;
    }
    public void Init(SkillManager skillManager, CameraManager cameraManager, InputHandler input)
    {
        CreateInternalObjects();
        BindEvents();

        _attackExecutor?.Init(this, skillManager);

        _attack?.Init(_playerStateManager, input, _attackExecutor, _modeController, _playerAnimationController);

        _move?.Init(
           _playerStateManager,
           input,
           cameraManager,
           _moveData,
           this,
           _modeController,
           _playerAnimationController,
           _attack);

        _playerAnimationController.Init(_playerStateManager, _modeController);

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Register(this, HitStopTargetGroup.Player);
        }
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public void Healing(float amount)
    {
        if (_playerStateManager.IsDead()) { return; }

        _playerStats.Heal(amount);
    }

    public void TakeDamage(float damage)
    {
        if (_playerStateManager.IsDead()) { return; }
        if (_playerStateManager.IsDodging()) { return; }

        int reductDamage = DamageSystem.ApplyDamageReduction(damage, _playerStats.DefensePower);

        _playerStats.TakeDamage(reductDamage);
    }
    public bool TryUseStamina(float amount)
    {
        return _playerStats.UseStamina(amount);
    }

    public float GetDodgeStaminaCost()
    {
        return _playerData.DodgeStaminaCost;
    }

    public void AddAttackPower(float value)
    {
        _playerStats.AddAttackPower(value);
    }

    public void AddCriticalRate(float value)
    {
        _playerStats.AddCriticalRate(value);
    }

    public void AddDefensePower(float value)
    {
        _playerStats.AddDefensePower(value);
    }

    public void AddMaxHealth(float value)
    {
        _playerStats.AddMaxHealth(value);
    }

    public void AddMaxStamina(float value)
    {
        _playerStats.AddMaxStamina(value);
    }

    public void OnSpeedChange(float timeScale)
    {
        TimeScale = timeScale;
        _playerAnimationController.SetAnimSpeed(timeScale);
        _move.SetTimeScale(timeScale);
    }

    [SerializeField] private PlayerData _playerData;
    [SerializeField] private MoveData _moveData;
    [SerializeField] private PlayerMovement _move;
    [SerializeField] private PlayerAttack _attack;
    [SerializeField] private AttackExecutor _attackExecutor;
    [SerializeField] private PlayerModeController _modeController;
    [SerializeField] private PlayerAnimationController _playerAnimationController;
    [SerializeField] private Transform _targetCenter;

    private PlayerStateManager _playerStateManager;
    private PlayerStats _playerStats;

    private void Update()
    {
        RegenerateStamina();
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {

            hitStopManager.Unregister(this, HitStopTargetGroup.Player);
        }

        if (_playerStats != null)
        {
            _playerStats.OnDead -= OnPlayerDead;
        }

        if (_move != null)
        {
            _move.OnEndDodge -= _attack.FinishDodge;
        }

        if (_playerAnimationController != null)
        {
            _playerAnimationController.OnDestroy();
        }
    }

    private void CreateInternalObjects()
    {
        _playerStateManager = new PlayerStateManager();
        _playerStats = new PlayerStats(_playerData);
    }

    private void BindEvents()
    {
        _playerStats.OnDead += OnPlayerDead;

        if (_move != null && _attack != null)
        {
            _move.OnEndDodge += _attack.FinishDodge;
        }
    }

    private void RegenerateStamina()
    {
        _playerStats.RegenerateStamina(_playerData.StaminaRegenPerSecond);
    }

    private void OnPlayerDead()
    {
        _playerStateManager.ChangeState(PlayerState.Dead);
        OnDead?.Invoke();
    }

    // デバッグ用
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 50, 500, 300), $"残りHP：{_playerStats.CurrentHealth}");
        GUI.Label(new Rect(10, 100, 500, 300), $"残りスタミナ：{_playerStats.CurrentStamina}");
    }

}
