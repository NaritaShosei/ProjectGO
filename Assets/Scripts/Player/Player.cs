using System;
using UnityEngine;

public class Player : MonoBehaviour, IPlayer, IStamina
{
    public event Action OnDead;
    public void Init(SkillManager skillManager, CameraManager cameraManager, InputHandler input)
    {
        _attackExecutor?.Init(_playerStats, skillManager);

        _move?.Init(
           _playerStateManager,
           input,
           cameraManager,
           _moveData,
           this,
           _modeController,
           _playerAnimationController);

        _attack?.Init(_playerStateManager, input, _attackExecutor, _modeController);

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

        // TODO:被弾ダメージ計算を考慮する
        _playerStats.TakeDamage(damage);
    }
    public bool TryUseStamina(float amount)
    {
        return _playerStats.UseStamina(amount);
    }

    public float GetDodgeStaminaCost()
    {
        return _playerData.DodgeStaminaCost;
    }

    [SerializeField] private PlayerMovement _move;
    [SerializeField] private PlayerAttack _attack;
    [SerializeField] private AttackExecutor _attackExecutor;
    [SerializeField] private PlayerModeController _modeController;
    [SerializeField] private MoveData _moveData;
    [SerializeField] private PlayerData _playerData;
    [SerializeField] private Transform _targetCenter;
    [SerializeField] private PlayerAnimationController _playerAnimationController;

    private PlayerStateManager _playerStateManager;
    private PlayerStats _playerStats;

    private void Awake()
    {
        InitInternal();
    }

    private void Update()
    {
        RegenerateStamina();
    }

    private void OnDestroy()
    {
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

    private void InitInternal()
    {
        CreateInternalObjects();
        BindEvents();
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
