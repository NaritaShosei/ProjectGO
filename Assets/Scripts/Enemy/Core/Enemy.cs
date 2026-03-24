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
    public event Action<IEnemy> OnDamaged;
    public event Action<IEnemy> OnArmorBroken;

    public event Action<float, float> OnHealthChanged
    {
        add => _stats.OnHealthChanged += value;
        remove => _stats.OnHealthChanged -= value;
    }

    public event Action<DamagePopupViewModel> OnDamageDealt;

    public virtual IEnemyConditionController ConditionController { get; }
    public IEnemyAnimator EnemyAnimator => _enemyAnimator;

    public Vector3 Position { get => transform.position; }

    public int Id => GetInstanceID();

    public virtual bool IsBoss => false;

    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// HitStop等でTimeScaleが変化したときに呼ばれる
    /// </summary>
    public void OnSpeedChange(float scale)
    {
        TimeScale = scale;
        // HitStop中はアニメーション再生も停止させる
        _enemyAnimator?.SetAnimSpeed(scale);
    }

    /// <summary>
    /// プレイヤー参照を受け取って初期化する
    /// </summary>
    public virtual void Init(IPlayer player)
    {
        _playerTransform = player.GetTargetCenter();
    }

    /// <summary>
    /// ノックバックの力を方向ベクトルとして直接座標に加算する
    /// </summary>
    public void AddKnockbackForce(Vector3 direction)
    {
        transform.position += direction;
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
        if (_isDead) { return; }

        int damage = DamageSystem.Calculate(context, _defenceContext);

        _stats.TakeDamage(damage);

        bool isKill = _stats.CurrentHealth <= 0;

        // 弱点ヒットは生身かつ雷神モード攻撃時のみ有効
        bool isWeakPoint = _defenceContext.EnemyType == EnemyType.Flesh
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

        var newSlot = _services.AttackerSlot;
        if (newSlot != null) newSlot.OnSlotReleased += HandleSlotReleased;
    }

    [SerializeField] protected EnemyData _data;
    [SerializeField] private Transform _targetCenter;
    [SerializeField] protected Animator _animator;

    // Turn用プロファイル（派生クラスのInspectorから設定する）
    [SerializeField] protected TurnProfile _turnProfile;

    // DistanceProfile（派生クラスのInspectorから設定する）
    [SerializeField] protected DistanceProfile _distanceProfile;

    // EnemyAnimatorと同じGameObjectにアタッチされたReceiverへの参照
    [SerializeField] private EnemyAnimationEventReceiver _animationEventReceiver;

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

    // 最後に受けたダメージの方向（死亡ノックバック用）
    protected Vector3 _lastHitDirection;


    protected CancellationTokenSource _shockCts;

    // サービス参照（InjectServicesで注入される）
    protected EnemyServices _services;

    protected virtual void Awake()
    {
        // OnDead時の登録
        OnDead += HandleDead;

        // 鎧の有無はMobEnemy.Init()で上書きされる。ここではデフォルト値として生身を設定する
        _defenceContext = new EnemyDefenseContext()
        {
            EnemyType = EnemyType.Flesh,
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
    }

    protected virtual void OnDeathInternal()
    {
        // 死亡アニメーション完了を待ってから破棄する
        // TODO: ObjectPool導入時は WaitForDeadAnimationAndDeactivate() に切り替える
        WaitForDeadAnimationAndDestroy().Forget();
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
            _services.AttackerSlot.TryAcquire(Id, 1, IsBoss);
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
    /// ObjectPoolからSetActive(true)した直後に呼ぶこと。
    /// </summary>
    public virtual void ReInitialize(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        _isDead = false;
        _deadAnimationEnded = false;
        TimeScale = 1f;

        // Animatorを初期状態（Idle）に戻す
        // Dead→Exit遷移が再活性化時に誤発火しないよう即時反映する
        _animator.Play("Idle", 0, 0f);
        _animator.Update(0f);

        // TODO: _stats.ResetHP() — EnemyStatsにリセットメソッドが追加されたら呼ぶ
    }

    /// <summary>
    /// 死亡アニメーション完了を待ってからGameObjectを破棄する。
    /// destroyCancellationToken により外部からの強制破棄にも対応する。
    /// </summary>
    /// <remarks>
    /// ObjectPool導入時は gameObject.SetActive(false) に置き換え、
    /// WaitForDeadAnimationAndDeactivate() にリネームして切り替えること。
    /// </remarks>
    private async UniTaskVoid WaitForDeadAnimationAndDestroy()
    {
        if (_animationEventReceiver == null)
        {
            Destroy(gameObject);
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

        if (this == null) return;
        Destroy(gameObject);
    }

    protected abstract void UpdateEnemy(float deltaTime);

}

