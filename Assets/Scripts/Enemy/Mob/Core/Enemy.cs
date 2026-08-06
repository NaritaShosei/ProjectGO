using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Enemyの基底クラス
/// </summary>
public abstract class Enemy : MonoBehaviour, IEnemy, ISpeedChange, IPoolable,IEnemySpawnState
{
    public event Action<IEnemy> OnDead;
    public event Action<IEnemy> OnDamaged;
    public event Action<IEnemy> OnArmorBroken;
    public event Action<Enemy> OnReleaseRequested;

    public event Action<float, float> OnHealthChanged
    {
        add => _stats.OnHealthChanged += value;
        remove => _stats.OnHealthChanged -= value;
    }

    public event Action<DamagePopupViewModel> OnDamageDealt;

    //HitEffect発生を外部（レシーバーやエフェクトマネージャーなど）に通知するイベント
    public event Action<HitEffectContext> OnHitEffect;

    //SE関係のイベント
    public event Action OnAttackSE;
    public event Action OnBarkSE;
    public event Action OnDownSE;

    public virtual IEnemyConditionController ConditionController { get; }
    public IEnemyAnimator EnemyAnimator => _enemyAnimator;
    public EnemyType EnemyType => _enemyType;

    public Transform Self { get => transform; }

    public int Id => GetInstanceID();

    public virtual bool IsBoss => false;

    public float TimeScale => _timeScale;

    public bool IsDead => _isDead;

    public bool IsLockable => !IsDead;

    public bool CanTakeDamage { get; private set; } = true;
    public bool CanReceiveCondition { get; private set; } = true;

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 所属Poolのキー_返却の参照に使用
    /// </summary>
    public string PoolKey => _poolKey;


    /// <summary>
    /// HitStop等でTimeScaleが変化したときに呼ばれる
    /// </summary>
    public void OnSpeedChange(float scale)
    {
        _timeScale = scale;
        // HitStop中はアニメーション再生も停止させる
        _enemyAnimator?.SetAnimSpeed(scale);
    }

    /// <summary>
    /// 初期化する
    /// </summary>
    public virtual void Init()
    {
        
        if (IsInitialized) return;

        IsInitialized = true;
    }

    /// <summary>
    /// FormationSystemへの登録完了後に呼ばれる。
    /// フォーメーション登録後に必要な初期化処理を行うためのフック。
    /// </summary>
    public virtual void OnRegisteredToFormation()
    {
    }

    /// <summary>
    /// ノックバックの力を方向ベクトルとして直接座標に加算する
    /// </summary>
    public void AddKnockbackForce(Vector3 direction)
    {
        // ノックバックも通常移動と同じ入口を通し、高速で壁を越えるのを防ぐ。
        Move(direction);
    }

    /// <summary>
    /// 壁との衝突を考慮して敵を移動させる。
    /// </summary>
    public void Move(Vector3 displacement)
    {
        if (_movementCollider != null && _services.WallAvoidanceService != null)
        {
            // 前フレームの数値誤差などですでに壁へ食い込んでいると、
            // BoxCastが正しいヒット距離を返せないため、先に壁の外へ戻す。
            transform.position = _services.WallAvoidanceService.ResolveSpawnPosition(
                _movementCollider,
                transform.position
            );

            // このフレームで進みたい距離を、壁の直前までに制限する。
            // 上下方向はノックバックの放物線に必要なので、水平移動だけが制限される。
            displacement = _services.WallAvoidanceService.ClampMovement(
                _movementCollider.bounds,
                displacement
            );
        }

        // 壁判定後に確定した安全な移動量を適用する。
        transform.position += displacement;

        if (_movementCollider != null && _services.WallAvoidanceService != null)
        {
            // 薄い壁や角、浮動小数点誤差によって移動後に重なりが残った場合の最終防御。
            transform.position = _services.WallAvoidanceService.ResolveSpawnPosition(
                _movementCollider,
                transform.position
            );
        }
    }

    /// <summary>
    /// UI等が攻撃・ヘルスバーのアンカーとして使用するTransformを返す
    /// </summary>
    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public virtual void TakeDamage(DamageContext context)
    {
        if (_isDead || !CanTakeDamage) { return; }

        int damage = DamageSystem.CalculateDamage(context, _defenceContext);

        _stats.TakeDamage(damage);

        bool isKill = _stats.CurrentHealth <= 0;

        // 弱点ヒットは生身かつ雷神モード攻撃時のみ有効
        bool isWeakPoint = _defenceContext.EnemyType == EnemyDefenceType.Flesh
            && context.PlayerMode == PlayerMode.Thunder;

        context.OnHitResult?.Invoke(
            new HitResult
            {
                IsKill = isKill,
                IsArmorBreak = false,
                IsWeakPoint = isWeakPoint
            });

        InvokeOnDamageDealt(damage, isWeakPoint, context.IsCritical);

        if (!isKill) InvokeOnDamaged();
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

    /// <summary>
    /// 座標を直接セットする（スポーン・テレポート等）
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// EnemyDataを外部から上書きする。
    /// Init()より前に呼ぶこと。
    /// </summary>
    public void SetData(EnemyData data)
    {
        if (data == null)
        {
            Debug.LogWarning($"{name}: SetData に null が渡されました");
            return;
        }

        if (IsInitialized)
        {
            Debug.LogWarning($"{name}: Init済みインスタンスへの SetData は反映されない可能性があります");
        }

        _data = data;
    }

    /// <summary>
    /// OnArmorBroken を派生クラスから発火するためのラッパー
    /// </summary>
    protected void InvokeOnArmorBroken() => OnArmorBroken?.Invoke(this);

    /// <summary>
    /// OnDamaged を派生クラスから発火するためのラッパー
    /// </summary>
    protected void InvokeOnDamaged() => OnDamaged?.Invoke(this);

    /// <summary>
    /// ダメージポップアップ表示用イベントを発火する
    /// </summary>
    protected void InvokeOnDamageDealt(int damage, bool isWeakPoint, bool isCritical)
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

    /// <summary>
    /// ヒットエフェクト表示用イベントを発火する
    /// </summary>
    /// <param name="context"></param>
    protected void InvokeOnHitEffect(HitEffectContext context)
    {
        OnHitEffect?.Invoke(context);
    }


    public abstract void OnConditionInterrupt();

    /// <summary>
    /// 各サービスをBehaviourに注入する
    /// EnemyManagerのSpawnから呼ぶ想定
    /// </summary>
    public void InjectServices(EnemyServices services)
    {
        // 再注入時に旧スロットの購読が残らないよう先に解除する
        var oldSlot = _services.AttackerSlot;
        if (oldSlot != null) oldSlot.OnSlotReleased -= HandleSlotReleased;

        _services = services;
        _playerTransform = _services.PlayerInformationService.Player.GetTargetCenter();

        var newSlot = _services.AttackerSlot;
        if (newSlot != null) newSlot.OnSlotReleased += HandleSlotReleased;
    }

    /// <summary>
    /// EnemyがObjectPoolからGetされたときの初期化処理
    /// </summary>
    public void OnGet()
    {
        enabled = true;
    }

    /// <summary>
    /// EnemyがObjectPoolにReleaseされたときのクリーンアップ処理
    /// </summary>
    public void OnRelease()
    {
        enabled = false;
    }

    /// <summary>
    /// 所属プールキーの設定
    /// </summary>
    /// <param name="key"></param>
    public void SetPoolKey(string key)
    {
        _poolKey = key;
    }

    public void SetSpawnState(bool active)
    {
        CanTakeDamage = !active;
        CanReceiveCondition = !active;
    }

    [SerializeField] protected EnemyData _data;
    [SerializeField] private Transform _targetCenter;
    [SerializeField] private Collider _movementCollider;
    [SerializeField] protected Animator _animator;

    // Turn用プロファイル（派生クラスのInspectorから設定する）
    [SerializeField] protected TurnProfile _turnProfile;

    // DistanceProfile（派生クラスのInspectorから設定する）
    [SerializeField] protected DistanceProfile _distanceProfile;

    // EnemyAnimatorと同じGameObjectにアタッチされたReceiverへの参照
    [SerializeField] private EnemyAnimationEventReceiver _animationEventReceiver;

    //SEのハンドラー
    [SerializeField]private EnemySoundHandler _soundHandler;

    [SerializeField]private EnemyType _enemyType;

    [Header("演出関係")]
    [SerializeField,Tooltip("スポーンエフェクトを適応の可否")]private bool _useSpawnEffect = true;
    [SerializeField,Tooltip("スポーンエフェクトのKey")] private string _spawnEffectKey = "スポーンエフェクト";

    private float _timeScale = 1f;

    protected EnemyDefenseContext _defenceContext;
    protected EnemyStats _stats;
    protected Transform _playerTransform;
    private HitStopManager _hitStopManager;

    protected IEnemyAnimator _enemyAnimator;

    // 死亡アニメーション終了待機のタイムアウト上限（秒）
    private const float _deadAnimationTimeout = 5f;

    protected bool _isDead;
    // 死亡アニメーション終了フラグ
    private bool _deadAnimationEnded;

    //スポーンのアニメーション
    private bool _useSpawnAnimation = true;

    // 最後に受けたダメージの方向（死亡ノックバック用）
    protected Vector3 _lastHitDirection;


    protected CancellationTokenSource _shockCts;

    // サービス参照（InjectServicesで注入される）
    protected EnemyServices _services;

    //Poolの所属を識別するためのキー
    private string _poolKey;


    protected virtual void Awake()
    {
        if (_movementCollider == null)
            _movementCollider = GetComponent<Collider>();

        // OnDead時の登録
        OnDead += HandleDead;

        // 鎧の有無はMobEnemy.Init()で上書きされる。ここではデフォルト値として生身を設定する
        _defenceContext = new EnemyDefenseContext()
        {
            EnemyType = EnemyDefenceType.Flesh,
            HasShockDebuff = false
        };

        _stats = new EnemyStats(_data);

        _stats.OnHealthZero += _stats.Kill;

        _stats.OnDead += OnDeath;

        // 鎧生成関連はすべてMobEnemyのほうで実装

        // EnemyAnimatorを生成する（Receiverを渡してイベント中継を設定する）
        _enemyAnimator = new EnemyAnimator(_animator, _animationEventReceiver);
        // 死亡アニメーション終了イベントを購読する
        _enemyAnimator.OnDeadEnd += HandleDeadEnd;
        _enemyAnimator.OnSpawnEffect += HandleSpawnEffect;

        if (_animationEventReceiver != null)
        {
            _animationEventReceiver.OnAttackEffect += HandleAttackEffect;
        }

        _enemyAnimator.OnSpawnEnd += HandleSpawnEnd;
        _soundHandler?.Init(this);
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
            // AllEnemies: 全敵を対象とするHitStop（クリティカル等）用
            _hitStopManager.Register(this, HitStopTargetGroup.AllEnemies);
            // HitEnemy: ヒットした1体のみを対象とするHitStop用
            // 全敵を登録しておき、HitStopManager側で対象の1体だけにフィルタリングする
            _hitStopManager.Register(this, HitStopTargetGroup.HitEnemy);
        }
    }

    private void OnDisable()
    {
        _hitStopManager?.UnregisterFromAll(this);
    }

    protected virtual void OnDestroy()
    {
        _stats.OnHealthZero -= _stats.Kill;
        _stats.OnDead -= OnDeath;
        OnDead -= HandleDead;

        var slot = _services.AttackerSlot;
        if (slot != null) slot.OnSlotReleased -= HandleSlotReleased;

        // 死亡アニメーション終了イベントの購読解除とDisposeをまとめて行う
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnDeadEnd -= HandleDeadEnd;
            _enemyAnimator.Dispose();
        }

        if (_animationEventReceiver != null)
        {
            _animationEventReceiver.OnAttackEffect -= HandleAttackEffect;
        }
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

    private void HandleDead(IEnemy _)
    {
        // _shockCtsの解除
        _shockCts?.Cancel();
        _shockCts?.Dispose();
        _shockCts = null;
    }

    protected virtual void OnDeathInternal()
    {
        // 死亡アニメーション完了を待ってから破棄する
        // (済み): ObjectPool導入時は WaitForDeadAnimationAndDeactivate() に切り替える
        WaitForDeadAnimationAndDeactivate().Forget();
    }

    protected virtual void HandleAttackEffect()
    {
        OnAttackSE?.Invoke();
    }

    protected void InvokeOnBarkSE()
    {
        OnBarkSE?.Invoke();
    }

    protected void InvokeOnDownSE()
    {
        OnDownSE?.Invoke();
    }

    protected virtual void HandleSpawnEffect()
    {
        if (!_useSpawnEffect) return;
        if (!ServiceLocator.TryGet(out EffectManager effectManager)) return;

        Debug.Log("スポーンエフェクト発火");
        effectManager.PlayEffect(
            _spawnEffectKey,
            transform.position);
    }

    /// <summary>
    /// スロット解放通知を受けてスロットの再取得を試みる
    /// すでに取得済みの場合は何もしない
    /// </summary>
    private void HandleSlotReleased()
    {
        if (_isDead) return;
        if (_services.AttackerSlot == null) return;
        if (_data == null) return;
        if (_data.AttackPatterns == null || _data.AttackPatterns.Count == 0) return;

        if (!_services.AttackerSlot.IsAcquired(Id))
        {
            _services.AttackerSlot.TryAcquire(Id, 1);
        }
    }

    /// <summary>
    /// 死亡アニメーション終了イベントのハンドラ
    /// </summary>
    private void HandleDeadEnd()
    {
        _deadAnimationEnded = true;
    }

    /// <summary>
    /// スポーン位置を指定して敵をプールから再利用するための初期化を行う。
    /// (済み)ObjectPoolからSetActive(true)した直後に呼ぶこと。
    /// </summary>
    public virtual void ReInitialize(Vector3 spawnPosition)
    {
        // まず指定された座標へ配置し、直後に壁との重なりを解消する。
        // CircleSpawnなどが壁の内側を指定しても、そのまま行動を開始させない。
        transform.position = spawnPosition;
        ResolveSpawnPosition();
        _isDead = false;
        _deadAnimationEnded = false;
        _timeScale = 1f;

        _stats.ResetHP(_data.MaxHP);

        // Animatorを初期状態（Idle）に戻す
        // Dead→Exit遷移が再活性化時に誤発火しないよう即時反映する
        _animator.Play("Idle", 0, 0f);
        _animator.Update(0f);

        _useSpawnAnimation = true;
        // TODO(済み): _stats.ResetHP() — EnemyStatsにリセットメソッドが追加されたら呼ぶ
    }

    /// <summary>
    /// 現在位置がWallレイヤーのコライダーと重なっていれば、壁の外へ押し戻す。
    /// スポーン直後と、移動前後の貫通補正から使用する。
    /// </summary>
    public void ResolveSpawnPosition()
    {
        if (_movementCollider == null || _services.WallAvoidanceService == null)
            return;

        transform.position = _services.WallAvoidanceService.ResolveSpawnPosition(
            _movementCollider,
            transform.position
        );
    }

    public virtual void PlaySpawnAnimation()
    {
        if (_useSpawnAnimation)
        {
            _animator.Play("Spawn", 0, 0f);
        }
        _useSpawnAnimation = false;
    }

    public virtual void HandleSpawnEnd()
    {
    }

    /// <summary>
    /// 死亡アニメーション完了を待ってからGameObjectを破棄する。
    /// destroyCancellationToken により外部からの強制破棄にも対応する。
    /// </summary>
    /// <remarks>
    /// ObjectPool導入時は gameObject.SetActive(false) に置き換え、
    /// WaitForDeadAnimationAndDeactivate() にリネームして切り替えること。
    /// </remarks>
    private async UniTaskVoid WaitForDeadAnimationAndDeactivate()
    {
        ServiceLocator.TryGet<EXPItemManager>(out var expManager);

        if (_animationEventReceiver == null)
        {
            ReleaseToPool();
            return;
        }

        // 前回の状態が残っている場合に備えてフラグをリセットする
        _deadAnimationEnded = false;

        try
        {
            // タイムアウト付きで死亡アニメーション終了を待機する
            // Dead→Exit遷移が正常に発火しない場合でも上限時間で強制破棄する
            await UniTask.WaitUntil(
                () => _deadAnimationEnded,
                cancellationToken: destroyCancellationToken
            ).TimeoutWithoutException(TimeSpan.FromSeconds(_deadAnimationTimeout));
        }
        catch (OperationCanceledException)
        {
            // destroyCancellationToken キャンセル時（シーン破棄など）は何もしない
            return;
        }

        expManager?.DropEXP(Self.position, _data.ExpDropAmount);

        if (this == null) return;
        ReleaseToPool();
    }

    /// <summary>
    /// Pool返却通知
    /// </summary>
    private void ReleaseToPool()
    {
        if (OnReleaseRequested == null)
        {
            Debug.LogError($"{name}: OnReleaseRequested に購読者がいないため、Pool返却できません。");
            gameObject.SetActive(false);
            return;
        }
        OnReleaseRequested?.Invoke(this);
    }

    protected abstract void UpdateEnemy(float deltaTime);

}

