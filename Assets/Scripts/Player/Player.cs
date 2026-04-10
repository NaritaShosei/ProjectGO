using System;
using UnityEngine;

public class Player : MonoBehaviour, IPlayer, ISpeedChange
{
    // ---- IPlayerStats 実装 ----
    public float AttackPower => _playerStats.AttackPower;
    public float CriticalRate => _playerStats.CriticalRate;
    public float DefensePower => _playerStats.DefensePower;
    public float MaxHealth => _playerStats.MaxHealth;
    public float CurrentHealth => _playerStats.CurrentHealth;
    public float MaxThunderGauge => _playerStats.MaxThunderGauge;
    public float CurrentThunderGauge => _playerStats.CurrentThunderGauge;
    public float InitialMaxThunderGauge => _playerStats.InitialMaxThunderGauge;

    public float TimeScale { get; set; } = 1f;

    public event Action OnDead;

    public event Action<float, float, float> OnHealthChanged
    {
        add => _playerStats.OnHealthChanged += value;
        remove => _playerStats.OnHealthChanged -= value;
    }

    public event Action<float, float, float> OnThunderGaugeChanged
    {
        add => _playerStats.OnThunderGaugeChanged += value;
        remove => _playerStats.OnThunderGaugeChanged -= value;
    }

    public void Init(SkillManager skillManager, InputHandler input)
    {
        BindEvents();

        _modeController.Init(_playerStats);

        _attackExecutor?.Init(this, skillManager);
        _attack?.Init(_playerStateManager, input, _attackExecutor, _modeController, _playerAnimationController);
        _interactor?.Init(_playerStateManager, input);

        _move?.Init(
            _playerStateManager,
            input,
            _moveData,
            _modeController,
            _playerAnimationController,
            _attack);

        _playerAnimationController.Init(_playerStateManager, _modeController);

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
            hitStopManager.Register(this, HitStopTargetGroup.Player);

        _interactor?.SearchLoop(destroyCancellationToken).Forget();
    }

    public Transform GetTargetCenter() => _targetCenter;

    public void Healing(float amount)
    {
        if (_playerStateManager.IsDead()) return;
        _playerStats.Heal(amount);
    }

    /// <summary>
    /// ダメージを受ける。回避中は無敵。被弾時はDamagedステートへ遷移。
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_playerStateManager.IsDead()) return;
        if (_playerStateManager.IsDodging()) return;

        int reductDamage = DamageSystem.ApplyDamageReduction(damage, _playerStats.DefensePower);
        _playerStats.TakeDamage(reductDamage);

        if (!_playerStateManager.IsDead())
        {
            _attack?.InterruptByDamage(); // 攻撃内部状態を全てクリア
            _playerStateManager.ChangeState(PlayerState.Damaged);
        }
    }

    // ---- IStatUpgradable ----
    public void AddAttackPower(float value) => _playerStats.AddAttackPower(value);
    public void AddCriticalRate(float value) => _playerStats.AddCriticalRate(value);
    public void AddDefensePower(float value) => _playerStats.AddDefensePower(value);
    public void AddMaxHealth(float value) => _playerStats.AddMaxHealth(value);
    public void AddMaxThunderGauge(float value) => _playerStats.AddMaxThunderGauge(value);
    public void AddThunderDrainPerSecond(float delta) => _playerStats.AddDrainPerSecond(delta);
    public void AddThunderRecoverPerSecond(float delta) => _playerStats.AddRecoverPerSecond(delta);

    public void OnSpeedChange(float timeScale)
    {
        TimeScale = timeScale;
        _playerAnimationController.SetAnimSpeed(timeScale);
        _move.SetTimeScale(timeScale);
    }

    /// <summary>ロックオン対象を設定する（nullで解除）</summary>
    public void SetLockOnTarget(Transform target)
    {
        _move?.SetLockOnTarget(target);
    }

    [SerializeField] private PlayerData _playerData;
    [SerializeField] private MoveData _moveData;
    [SerializeField] private PlayerMovement _move;
    [SerializeField] private PlayerAttack _attack;
    [SerializeField] private PlayerInteractor _interactor;
    [SerializeField] private AttackExecutor _attackExecutor;
    [SerializeField] private PlayerModeController _modeController;
    [SerializeField] private PlayerAnimationController _playerAnimationController;
    [SerializeField] private Transform _targetCenter;

    private PlayerStateManager _playerStateManager;
    private PlayerStats _playerStats;

    private void Awake()
    {
        _playerStateManager = new PlayerStateManager();
        _playerStats = new PlayerStats(_playerData);
    }

    private void Update()
    {
        TickThunderGauge();
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
            hitStopManager.Unregister(this, HitStopTargetGroup.Player);

        if (_playerStats != null)
        {
            _playerStats.OnDead -= OnPlayerDead;
            _playerStats.OnThunderGaugeDepleted -= HandleThunderGaugeDepleted;
        }

        if (_move != null)
            _move.OnEndDodge -= _attack.FinishDodge;

        if (_playerAnimationController != null)
        {
            _playerAnimationController.OnModeChangeComplete -= OnModeChangeComplete;
            _playerAnimationController.OnDestroy();
        }
    }

    private void BindEvents()
    {
        _playerStats.OnDead += OnPlayerDead;
        _playerStats.OnThunderGaugeDepleted += HandleThunderGaugeDepleted;

        if (_move != null && _attack != null)
            _move.OnEndDodge += _attack.FinishDodge;

        if (_playerAnimationController != null && _playerStateManager != null)
            _playerAnimationController.OnModeChangeComplete += OnModeChangeComplete;
    }

    private void TickThunderGauge()
    {
        bool isThunderMode = _modeController != null
            && _modeController.CurrentMode == PlayerMode.Thunder
            && _playerStateManager.CurrentState != PlayerState.ModeChanging;
        _playerStats.TickThunderGauge(Time.deltaTime * TimeScale, isThunderMode);
    }

    private void HandleThunderGaugeDepleted()
    {
        if (_modeController.CurrentMode == PlayerMode.Thunder)
            _modeController.SwitchMode(PlayerMode.Warrior);
    }

    private void OnModeChangeComplete()
    {
        if (_playerStateManager.CurrentState == PlayerState.ModeChanging)
            _playerStateManager.ChangeState(PlayerState.Idle);
    }

    private void OnPlayerDead()
    {
        _playerStateManager.ChangeState(PlayerState.Dead);
        OnDead?.Invoke();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 50, 500, 300), $"残りHP：{_playerStats.CurrentHealth}");
        GUI.Label(new Rect(10, 100, 500, 300), $"雷ゲージ：{_playerStats.CurrentThunderGauge:F1} / {_playerStats.MaxThunderGauge:F1}");
    }
}
