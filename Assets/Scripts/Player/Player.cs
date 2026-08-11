using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
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
    public float BaseMaxThunderGauge => _playerStats.InitialMaxThunderGauge;

    public float TimeScale => _timeScale;

    public float BaseAttackPower => _playerData.AttackPower;

    public float BaseCriticalRate => _playerData.CriticalRate;

    public float BaseDefensePower => _playerData.DefensePower;

    public float BaseMaxHealth => _playerData.Stats.MaxHealth;

    public PlayerMode CurrentMode => _modeController.CurrentMode;


    /// ダメージを受けたときのイベント。ダメージのコンテキスト情報を引数として渡す。
    public event Action<PlayerDamageEffectContext> OnDamagedEffect;

    /// <summary>
    /// 死亡直前イベント。
    /// true を返すと死亡をキャンセルする。
    /// </summary>
    public event Func<bool> OnBeforeDead
    {
        add => _playerStats.OnBeforeDead += value;
        remove => _playerStats.OnBeforeDead -= value;
    }


    public event Action OnJustDodgeSuccess;

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

    public event Action OnDead;

    public event Action<Transform> OnEndDodge;

    public void Init(SkillManager skillManager, InputHandler input)
    {
        BindEvents();

        _modeController.Init(_playerStats);

        _attackExecutor?.Init(this, skillManager);
        _attack?.Init(_playerStateManager, input, _attackExecutor, _modeController, _playerAnimationController, skillManager);
        _interactor?.Init(_playerStateManager, input);

        _move?.Init(
            _playerStateManager,
            input,
            _moveData,
            _modeController,
            _playerAnimationController,
            _attack);

        _playerAnimationController.Init(_playerStateManager, _modeController);

        _soundHandler?.Init(
            _playerAnimationController,
            _playerStateManager,
            _modeController,
            _attackExecutor,
            _attack,
            this);

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Register(this, HitStopTargetGroup.Player);
            hitStopManager.Register(_thunderGaugeSpeedTarget, HitStopTargetGroup.ThunderGauge);
        }

        _interactor?.SearchLoop(destroyCancellationToken).Forget();
    }

    public Transform GetTargetCenter() => _targetCenter;

    public void Healing(float amount)
    {
        if (_playerStateManager.IsDead()) return;
        _playerStats.Heal(amount);
    }

    /// <summary>
    /// ダメージを受ける。ダメージリアクションの発生は、状態や修正によって制御される。
    /// </summary>
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, DamageReactionType.Small);
    }

    public void TakeDamage(float damage, DamageReactionType reactionType)
    {
        if (_playerStateManager.IsDead()) return;

        if (CurrentMode == PlayerMode.Thunder
            && _justDodgeSystem != null
            && _justDodgeSystem.TryJustDodge())
        {
            Debug.Log("ジャスト回避成功");
            return;
        }

        if (_playerStateManager.IsInvincible()) return;

        // ダメージ修正を全て適用する。これにより、特定の条件で受けるダメージを増減させることができる。
        foreach (var mod in _damageModifiers)
        {
            mod.Modify(ref damage, CurrentMode);
        }

        int reductDamage = DamageSystem.ApplyDamageReduction(damage, DefensePower);
        _playerStats.TakeDamage(reductDamage);

        //ダメージエフェクトの通知
        OnDamagedEffect?.Invoke(
            new PlayerDamageEffectContext
            {
                HitPosition = _targetCenter.position
            });

        bool canInterrupt = true;

        // Modify を全て確認して、ダメージリアクションを発生させていいか判断する。
        // どれか一つでもダメージリアクションを発生させないと判断したら、リアクションは発生しない。
        foreach (var mod in _damageReactionModifiers)
        {
            if (!mod.CanInterrupt(
                _playerStateManager.CurrentState))
            {
                canInterrupt = false;
            }
        }

        // ダメージを受けたら、一定時間ダメージ無敵にする。これにより、連続でダメージを受けるのを防ぐ。
        _playerStateManager.AddInvincible(InvincibleType.Damaged);

        // ダメージ無敵を解除するタイミングは、プレイヤーデータで設定された時間経過後。これにより、ダメージを受けた後の無敵時間を柔軟に設定できる。
        HandleDamageInvincibilityEnd().Forget();

        // ダメージリアクションを発生させていいと判断された場合、状態をダメージ状態に遷移させる。      
        if (canInterrupt && !_playerStateManager.IsDead())
        {
            _attack?.InterruptByDamage(); // 攻撃内部状態を全てクリア
            _playerAnimationController.SetDamageReaction(reactionType);
            _move?.PlayDamageReaction(reactionType);
            _playerStateManager.ChangeState(PlayerState.Damaged);
        }
    }

    /// <summary>
    /// ステータスに修正を加える。バフ・デバフの適用などに使用。
    /// </summary>
    public void AddModifier(IStatModifier modifier)
    {
        // ここでは、回避無敵時間の修正は移動コンポーネントに渡し、それ以外の修正はプレイヤーステータスに渡す。
        // これにより、回避無敵時間の修正が移動ロジックに直接影響を与えるようになる。
        if (modifier.TargetStat == StatType.DodgeInvincibleTime)
        {
            _move.AddModifier(modifier);
        }
        else
        {
            _playerStats.AddModifier(modifier);
        }
    }

    /// <summary>
    /// ダメージリアクションを有効にするかどうかを判断する修正を加える。これにより、特定の状態でダメージリアクションを無効化することができる。
    /// </summary>
    public void AddDamageReactionModifier(IDamageReactionModifier modifier)
    {
        if (!_damageReactionModifiers.Contains(modifier))
            _damageReactionModifiers.Add(modifier);
    }

    /// <summary>
    /// ダメージ計算に影響を与える修正を加える。これにより、特定の条件で受けるダメージを増減させることができる。
    /// </summary>
    public void AddDamageModifier(IDamageModifier modifier)
    {
        if (!_damageModifiers.Contains(modifier))
            _damageModifiers.Add(modifier);
    }

    public void OnSpeedChange(float timeScale)
    {
        _timeScale = timeScale;
        _playerAnimationController.SetAnimSpeed(timeScale);
        _move.SetTimeScale(timeScale);
    }

    [Header("Data")]
    [SerializeField] private PlayerData _playerData;
    [SerializeField] private MoveData _moveData;

    [Header("参照")]
    [SerializeField] private PlayerMovement _move;
    [SerializeField] private PlayerAttack _attack;
    [SerializeField] private PlayerInteractor _interactor;
    [SerializeField] private AttackExecutor _attackExecutor;
    [SerializeField] private PlayerModeController _modeController;
    [SerializeField] private PlayerAnimationController _playerAnimationController;
    [SerializeField] private PlayerSoundHandler _soundHandler;
    [SerializeField] private Transform _targetCenter;
    [SerializeField] private JustDodgeSystem _justDodgeSystem;
    [SerializeField] private JustDodgeEffectPlayer _justDodgeEffectPlayer;

    private PlayerStateManager _playerStateManager;
    private PlayerStats _playerStats;
    private float _thunderGaugeTimeScale = 1f;
    private ThunderGaugeSpeedTarget _thunderGaugeSpeedTarget;

    private List<IDamageReactionModifier> _damageReactionModifiers = new List<IDamageReactionModifier>();
    private List<IDamageModifier> _damageModifiers = new List<IDamageModifier>();

    private CancellationTokenSource _damageInvincibilityCts;

    private float _timeScale = 1f;
    private void Awake()
    {
        _playerStateManager = new PlayerStateManager();
        _playerStats = new PlayerStats(_playerData);
        _thunderGaugeSpeedTarget = new ThunderGaugeSpeedTarget(this);
    }

    private void Update()
    {
        TickThunderGauge();
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet(out CameraManager cameraManager))
            cameraManager.OnLockOnTargetChanged -= SetLockOnTarget;

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Unregister(this, HitStopTargetGroup.Player);
            hitStopManager.Unregister(_thunderGaugeSpeedTarget, HitStopTargetGroup.ThunderGauge);
        }

        if (_playerStats != null)
        {
            _playerStats.OnDead -= OnPlayerDead;
            _playerStats.OnThunderGaugeDepleted -= HandleThunderGaugeDepleted;
        }

        if (_playerAnimationController != null)
        {
            _playerAnimationController.OnModeChangeComplete -= OnModeChangeComplete;
            _playerAnimationController.OnDestroy();
        }

        if (_move != null && _justDodgeSystem != null)
        {
            _move.OnStartDodgeInvincible -= _justDodgeSystem.JustDodgeWindowStart;
            _justDodgeSystem.OnJustDodgeSuccess -= HandleJustDodgeSuccess;
        }

        if (_move != null)
            _move.OnEndDodge -= RelayEndDodge;
    }

    private void BindEvents()
    {
        _playerStats.OnDead += OnPlayerDead;
        _playerStats.OnThunderGaugeDepleted += HandleThunderGaugeDepleted;

        if (_playerAnimationController != null && _playerStateManager != null)
            _playerAnimationController.OnModeChangeComplete += OnModeChangeComplete;

        if (_move != null && _justDodgeSystem != null)
        {
            _move.OnStartDodgeInvincible += _justDodgeSystem.JustDodgeWindowStart;
            _justDodgeSystem.OnJustDodgeSuccess += HandleJustDodgeSuccess;
        }

        if (_move != null)
            _move.OnEndDodge += RelayEndDodge;

        if (_justDodgeSystem != null)
            _justDodgeSystem.OnJustDodgeSuccess += RelayJustDodgeSuccess;

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
            cameraManager.OnLockOnTargetChanged += SetLockOnTarget;
    }

    /// <summary>
    /// 雷ゲージの管理。雷モード中は減少し、そうでないときは回復する。雷ゲージが0になると、強制的に戦士モードに切り替わる。
    /// </summary>
    private void TickThunderGauge()
    {
        bool isThunderMode = _modeController != null
            && _modeController.CurrentMode == PlayerMode.Thunder
            && _playerStateManager.CurrentState != PlayerState.ModeChanging;
        _playerStats.TickThunderGauge(Time.deltaTime * TimeScale * _thunderGaugeTimeScale, isThunderMode);
    }

    /// <summary>
    /// 雷ゲージが0になったときの処理。雷モード中であれば、戦士モードに切り替える。
    /// </summary>
    private void HandleThunderGaugeDepleted()
    {
        if (_modeController.CurrentMode == PlayerMode.Thunder)
            _modeController.SwitchMode(PlayerMode.Warrior);
    }

    /// <summary>
    /// モード切替アニメーションが完了したときの処理。モード切替中であれば、状態を待機状態に切り替える。
    /// </summary>
    private void OnModeChangeComplete()
    {
        if (_playerStateManager.CurrentState == PlayerState.ModeChanging)
            _playerStateManager.ChangeState(PlayerState.Idle);
    }

    /// <summary>ロックオン対象を設定する（nullで解除）</summary>
    private void SetLockOnTarget(ILockOnTarget target)
    {
        _playerAnimationController?.SetLockedOn(target != null);

        if (target == null || !target.IsLockable || target.GetTargetCenter() == null)
        {
            _move?.SetLockOnTarget(null);
            return;
        }

        _move?.SetLockOnTarget(target.GetTargetCenter());
    }

    /// <summary>
    /// ダメージ無敵の終了を処理する。プレイヤーデータで設定された時間経過後に、ダメージ無敵を解除する。
    /// </summary>
    private async UniTaskVoid HandleDamageInvincibilityEnd()
    {
        _damageInvincibilityCts?.Cancel();
        _damageInvincibilityCts?.Dispose();
        _damageInvincibilityCts = new CancellationTokenSource();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(
            _damageInvincibilityCts.Token, destroyCancellationToken);


        float elapsed = 0f;
        try
        {
            while (elapsed < _playerData.InvincibleDuration)
            {
                elapsed += Time.deltaTime * TimeScale;
                await UniTask.Yield(cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // オブジェクトが破壊された場合など、処理がキャンセルされたときは何もしない。
        }
        finally
        {
            _playerStateManager.RemoveInvincible(InvincibleType.Damaged);
        }
    }

    /// <summary>
    /// ジャスト回避成功時の処理。
    /// </summary>
    private void HandleJustDodgeSuccess()
    {
        var context = new JustDodgeContext();

        _justDodgeEffectPlayer?.Play(context);
    }

    private void OnPlayerDead()
    {
        _playerStateManager.ChangeState(PlayerState.Dead);
        OnDead?.Invoke();
    }

    private void RelayEndDodge()
    {
        OnEndDodge?.Invoke(transform);
    }

    private void RelayJustDodgeSuccess()
    {
        OnJustDodgeSuccess?.Invoke();
    }

    private sealed class ThunderGaugeSpeedTarget : ISpeedChange
    {
        public ThunderGaugeSpeedTarget(Player player)
        {
            _player = player;
        }

        public float TimeScale => _timeScale;

        public void OnSpeedChange(float scale)
        {
            _timeScale = scale;
            _player._thunderGaugeTimeScale = scale;
        }

        private readonly Player _player;
        private float _timeScale = 1f;
    }
}
