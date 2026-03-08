using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Enemyの基底クラス
/// </summary>
public abstract class Enemy : MonoBehaviour, IEnemy, ISpeedChange
{
    public event Action<IEnemy> OnDead;
    public event Action<IEnemy> OnArmorBroken;

    public event Action<float, float> OnHealthChanged
    {
        add => _stats.OnHealthChanged += value;
        remove => _stats.OnHealthChanged -= value;
    }

    // public event Action<DamagePopupViewModel> OnDamageDealt;

    public virtual EnemyConditionController ConditionController { get; }

    public Vector3 Position { get => transform.position; }

    public float TimeScale { get; set; } = 1f;

    public void OnSpeedChange(float scale)
    {
        TimeScale = scale;
    }

    public virtual void Init(IPlayer player)
    {
        _playerTransform = player.GetTargetCenter();
    }

    public void AddKnockBackForce(Vector3 direction)
    {
        transform.position += direction;
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public virtual void TakeDamage(DamageContext context)
    {
        if (_isDead) { return; }

        int damage = DamageSystem.Calculate(context, _defenceContext);

        _stats.TakeDamage(damage);

        bool isKill = _stats.CurrentHealth <= 0;
        bool isWeakPoint = _defenceContext.EnemyType == EnemyType.Flesh;

        context.OnHitResult?.Invoke(
            new HitResult
            {
                IsKill = isKill,
                IsArmorBreak = false,
                IsWeakPoint = isWeakPoint
            });

        // InvokeOnDamageDealt(damage, isWeakPoint, context.IsCritical);
    }

    public async UniTask ActivateShockDebuff(int durationSeconds = 10)
    {
        _shockCts?.Cancel();
        _shockCts?.Dispose();
        _shockCts = new CancellationTokenSource();

        _defenceContext.HasShockDebuff = true;

        float t = 0f;
        float duration = durationSeconds;

        try
        {
            while (t < duration)
            {
                t += Time.deltaTime * TimeScale;
                await UniTask.Yield(_shockCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }

        _defenceContext.HasShockDebuff = false;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    /*
    // MobEnemyからInvokeできないのでラップ？している
    public void InvokeOnDamageDealt(int damage, bool isWeakPoint, bool isCritical)
    {
        OnDamageDealt?.Invoke(
            new DamagePopupViewModel(
                damage: damage,
                isWeakPoint: isWeakPoint,
                isCritical: isCritical,
                worldPosition: GetTargetCenter().position
                )
            );
    }
    */

    public abstract void OnConditionInterrupt();

    /// <summary>
    /// 各サービスをBehaviourに注入する
    /// EnemyManagerのSpawnから呼ぶ想定
    /// </summary>
    public void InjectServices(
        ISpatialHashGrid spatialHashGrid,
        ISeparationService separationService,
        IWallAvoidanceService wallAvoidanceService,
        IEnemyAttackerSlot attackerSlot
    )
    {
        _spatialHashGrid = spatialHashGrid;
        _separationService = separationService;
        _wallAvoidanceService = wallAvoidanceService;
        _attackerSlot = attackerSlot;
    }

    [SerializeField] protected EnemyData _data;
    [SerializeField] private Transform _targetCenter;

    // Turn用プロファイル（派生クラスのInspectorから設定する）
    [SerializeField] protected TurnProfile _turnProfile;

    protected EnemyDefenseContext _defenceContext;
    protected EnemyStats _stats;
    protected Transform _playerTransform;
    private HitStopManager _hitStopManager;

    protected bool _isDead;

    protected CancellationTokenSource _shockCts;

    // サービス参照（InjectServicesで注入される）
    protected ISpatialHashGrid _spatialHashGrid;
    protected ISeparationService _separationService;
    protected IWallAvoidanceService _wallAvoidanceService;
    protected IEnemyAttackerSlot _attackerSlot;

    protected virtual void Awake()
    {
        // OnDead時の登録
        OnDead += HandleDead;

        // 雑に生身限定
        _defenceContext = new EnemyDefenseContext()
        {
            EnemyType = EnemyType.Flesh,
            HasShockDebuff = false
        };

        _stats = new EnemyStats(_data);

        _stats.OnHealthZero += _stats.Kill;

        _stats.OnDead += OnDeath;

        // 鎧生成関連はすべてMobEnemyのほうで実装
    }

    protected virtual void Update()
    {
        if (_isDead) return;

        float dt = Time.deltaTime * TimeScale;
        UpdateEnemy(dt);
    }

    private void OnEnable()
    {
        if (ServiceLocator.TryGet<HitStopManager>(out var manager))
        {
            _hitStopManager = manager;
            _hitStopManager.Register(this, HitStopTargetGroup.AllEnemies);
        }
    }

    private void OnDisable()
    {
        _hitStopManager?.UnregisterFromAll(this);
    }

    private void OnDestroy()
    {
        _stats.OnHealthZero -= _stats.Kill;

        _stats.OnDead -= OnDeath;

        OnDead -= HandleDead;
    }

    /// <summary>
    /// 死亡時処理
    /// </summary>
    protected virtual void OnDeath()
    {
        if (_isDead) { return; }

        _isDead = true;
        OnDead?.Invoke(this);
        OnDeathInternal();
    }

    protected void HandleDead(IEnemy _)
    {
        // _shockCtsの解除
        _shockCts?.Cancel();
        _shockCts?.Dispose();
    }

    protected virtual void OnDeathInternal()
    {
        Destroy(gameObject);
    }

    protected abstract void UpdateEnemy(float deltaTime);
}

// TODO: 鎧が壊れたことを検知してEnemyTypeを切り替える
public struct EnemyDefenseContext
{
    // 鎧 / 生身
    public EnemyType EnemyType;
    // 感電弱体化状態か
    public bool HasShockDebuff;
}

public enum EnemyType
{
    [InspectorName("生身")]
    Flesh,
    [InspectorName("鎧")]
    Armor,
}
