using System;
using UnityEngine;

public class Player : MonoBehaviour, IPlayer, IStamina
{
    public event Action OnDead;
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public void Init(SkillManager skillManager, CameraManager cameraManager, InputHandler input)
    {
        CreateInternalObjects();
        BindEvents();

        _attackExecutor?.Init(_playerStats, skillManager);

        _move?.Init(
           _playerStateManager,
           input,
           cameraManager,
           _moveData,
           this,
           _modeController,
           _playerAnimationController);

        _attack?.Init(_playerStateManager, input, _attackExecutor, _modeController, _playerAnimationController);

        _playerAnimationController.Init(_playerStateManager, _modeController);
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
        if (_playerStats != null)
        {
            _playerStats.OnDead -= OnPlayerDead;
            _playerStats.OnHealthChanged -= HealthChange;
            _playerStats.OnStaminaChanged -= StaminaChange;
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
        _playerStats.OnHealthChanged += HealthChange;
        _playerStats.OnStaminaChanged += StaminaChange;

        if (_move != null && _attack != null)
        {
            _move.OnEndDodge += _attack.FinishDodge;
        }
    }

    private void HealthChange(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }

    private void StaminaChange(float current, float max)
    {
        OnStaminaChanged?.Invoke(current, max);
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
