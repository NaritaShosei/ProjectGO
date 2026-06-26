using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

// NOTE:
// モブ敵のの基底クラスとして作成
// ・感電する
// ・鎧を登録できる
// ※鎧が残っているかはEnemyTypeで判定
// ※鎧持ちでも感電する

public class MobEnemy : Enemy, IFormationParticipant
{
    public override IEnemyConditionController ConditionController { get => _conditionController; }

    // Armor登録を外部（UI等）に通知するイベント
    // 購読者はIArmorHealth越しにHP変化・破壊を受け取る
    public event Action<IArmorHealth> OnArmorRegistered;

    // ─── IFormationParticipant ───────────────────────────────────────────
    public int EnemyId => GetInstanceID();
    public float CombatPower => _data != null ? _data.CombatPower : 0f;
    public int FormationSlotCost => 1;
    // _contextはInit後に生成されるためnullチェックが必要
    public bool IsInAttackCooldown => _context != null && _context.AttackCooldownRemaining > 0f;

    public override void Init()
    {
        _context = new EnemyRuntimeContext();
        _runner = new EnemyBehaviourRunner(this);
        _state = new EnemyStateContext();

        _conditionController = new EnemyConditionController(this);

        var initCtx = new BehaviourInitContext(this, _data, _playerTransform, _context, _enemyAnimator, _state);

        RegisterBehaviours(initCtx);

        // 鎧登録　データがなければ裸
        if (_armor != null)
        {
            _defenceContext.EnemyType = EnemyDefenceType.Armor;
            _armor.Init(this);
            _armor.OnBroken += BreakArmor;
            // Init()後に発火することで購読者がOnHealthChangedを安全に受け取れる
            InvokeArmorRegistered();
        }
        else
        {
            _defenceContext.EnemyType = EnemyDefenceType.Flesh;
        }
    }

    /// <summary>
    /// ObjectPoolから再利用する際の初期化。SetActive(true)直後に呼ぶこと。
    /// </summary>
    public override void ReInitialize(Vector3 spawnPosition)
    {
        base.ReInitialize(spawnPosition);

        //鎧の初期化
        if (_armor != null)
        {
            _armor.gameObject.SetActive(true);
            _defenceContext.EnemyType = EnemyDefenceType.Armor;

            _armor.OnBroken -= BreakArmor;
            _armor.OnBroken += BreakArmor;
        }

        // RuntimeContextをリセットする
        _context?.Reset();

        // Conditionをすべてクリアする
        _conditionController?.Clear();

        // BehaviourRunnerを初期状態に戻す
        _runner?.Reset();

        // TODO: _stats.ResetHP() — EnemyStatsにリセットメソッドが追加されたら呼ぶ
        // TODO: 鎧のリセット（ArmorStats）
        // TODO: アタッカースロットの再取得
    }

    public override void TakeDamage(DamageContext context)
    {
        if (_isDead) { return; }

        int damage = DamageSystem.CalculateDamage(context, _defenceContext);

        //ダメージ表示用に総ダメージを保存
        int showDamage = damage;

        bool armorWasAlive = _defenceContext.EnemyType == EnemyDefenceType.Armor;

        // 鎧がダメージを肩代わり
        if (_defenceContext.EnemyType == EnemyDefenceType.Armor)
        {
            if (_armor != null) damage = Mathf.FloorToInt(_armor.AbsorbDamageAndReturnExcess(damage));
        }

        bool isArmorBreak = armorWasAlive && _defenceContext.EnemyType == EnemyDefenceType.Flesh;

        // 弱点ヒットは生身かつ雷神モード攻撃時に有効
        bool isWeakPoint = (!armorWasAlive
            && _defenceContext.EnemyType == EnemyDefenceType.Flesh
            && context.PlayerMode == PlayerMode.Thunder)
            //鎧かつ闘神モードの時に有効
            || (armorWasAlive && context.PlayerMode == PlayerMode.Warrior);

        // 鎧に当たったか（鎧が生きていて、かつ鎧破壊が起きていない = 鎧が生き残った）
        bool isArmorHit = armorWasAlive && !isArmorBreak;

        InvokeOnDamageDealt(showDamage, isWeakPoint, context.IsCritical);

        //ヒットエフェクトの通知
        InvokeOnHitEffect(
            new HitEffectContext
            {
                Position = transform.position,
                PlayerMode = context.PlayerMode,
                IsArmorHit = isArmorHit,
                IsArmorBreak = isArmorBreak
            });

        //超過ダメージを生身に流す
        _stats.TakeDamage(damage);

        bool willKill = _stats.CurrentHealth <= 0;

        // -------- HitResult通知 --------
        context.OnHitResult?.Invoke(
            new HitResult
            {
                IsKill = willKill,
                IsArmorBreak = isArmorBreak,
                IsWeakPoint = isWeakPoint,
                IsArmorHit = isArmorHit,
            });

        if (!willKill) InvokeOnDamaged();

        // -------- 追加効果 --------

        ApplyAdditionalEffects(context);
    }

    public override void OnConditionInterrupt()
    {
        _runner.ForceExitAction();
    }

    /// <summary>
    /// Golmeのために追加
    /// Armorを再利用、再生成した後にOnBrokenイベントの再登録
    /// </summary>
    public void RebindArmor()
    {
        if (_armor == null)
        {
            return;
        }
        _armor.OnBroken -= BreakArmor;
        _armor.OnBroken += BreakArmor;
    }

    // Armorの登録
    [SerializeField] protected MobArmor _armor;

    protected EnemyBehaviourRunner _runner;
    private EnemyRuntimeContext _context;
    private EnemyStateContext _state;
    private EnemyConditionController _conditionController;
    protected MeleeAttackBehaviour _attack;
    protected TurnBehaviour _turn;
    protected BarkBehaviour _bark;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_armor != null) _armor.OnBroken -= BreakArmor;

        // BarkBehaviourのイベント購読を解除する
        _bark?.Dispose();
        // MeleeAttackBehaviourのイベント購読を解除する
        _attack?.Dispose();
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        if (_runner == null || _conditionController == null) { return; }

        // 攻撃クールダウンをTimeScale反映済みdeltaTimeで進める
        // Behaviourの実行状態に関わらず毎フレーム減算する
        if (_context.AttackCooldownRemaining > 0f)
        {
            _context.AttackCooldownRemaining -= deltaTime;
            if (_context.AttackCooldownRemaining < 0f) _context.AttackCooldownRemaining = 0f;
        }

        // スロット保持中にパターン未選択なら再選択する
        // スポーン時取得失敗後の再取得・攻撃終了後の再選択をここで一括処理する
        if (_services.AttackerSlot != null && _services.AttackerSlot.IsAcquired(Id) && _context.SelectedPattern == null)
        {
            _context.SelectedPattern = SelectPattern();
        }

        _conditionController.Tick(deltaTime);
        if (_conditionController.BlocksAction) { return; }
        _runner.Tick(deltaTime);
    }

    /// <summary>
    /// スロット解放・Behaviourの停止を行う。
    /// 死亡時と将来のプール返却時の両方から呼ぶ想定。
    /// </summary>
    protected virtual void OnDespawn()
    {
        _runner?.ForceExitAction();
        _attack?.ReleaseSlot();
    }

    protected override void OnDeathInternal()
    {
        OnDespawn();

        // SetDead() と物理ノックバックを DeadCondition に委譲する
        // ApplyImmediate を使い ConditionController 管理下に置く（Clear() でキャンセル可能にするため）
        _conditionController.ApplyImmediate(new DeadCondition(_lastHitDirection, _data, destroyCancellationToken));

        // ヒールアイテムのドロップ抽選を行う
        if (CheckProbability(_data.HealDropChance)
            && ServiceLocator.TryGet<ItemPickupManager>(out var itemSpawner))
        {
            itemSpawner.Spawn(transform.position);
        }

        base.OnDeathInternal();
    }

    protected virtual void RegisterBehaviours(BehaviourInitContext initCtx)
    {
        // TurnProfileが未設定の場合は警告を出してTurnを登録しない
        if (_turnProfile == null)
        {
            Debug.LogWarning($"{nameof(MobEnemy)}: TurnProfileが未設定です。Turnは無効になります。");
        }
        else
        {
            _turn = new TurnBehaviour(_turnProfile);
            _turn.Init(initCtx);
            _runner.RegisterTurn(_turn);
        }

        // AttackerSlotが未設定の場合は警告を出してAttackを登録しない
        if (_services.AttackerSlot == null)
        {
            Debug.LogWarning($"{nameof(MobEnemy)}: AttackerSlotが未注入です。Attackは無効になります。");
        }
        else if (_data.AttackPatterns == null || _data.AttackPatterns.Count == 0)
        {
            Debug.LogWarning($"{nameof(MobEnemy)}: AttackPatternsが空です。Attack・スロット取得をスキップします。");
        }
        else
        {
            _attack = new MeleeAttackBehaviour(_services, _animator, _distanceProfile);
            _attack.Init(initCtx);
            _runner.Register(_attack);

            // スポーン時にスロット取得を試みる
            // 満杯の場合は OnSlotReleased イベントで再試行される
            _services.AttackerSlot.TryAcquire(Id, 1);

            // BarkをattackerSlotブロック内に移動（nullチェック済みの範囲で登録）
            // distanceProfileがない場合はBarkも登録しない
            if (_distanceProfile != null)
            {
                _bark = new BarkBehaviour(_distanceProfile, _services, _data.BarkChance);
                _bark.Init(initCtx);
                _runner.Register(_bark);
            }
        }

        // DistanceProfileが未設定の場合は警告を出してMove・Bark・Roamを登録しない
        if (_distanceProfile == null)
        {
            Debug.LogWarning($"{nameof(MobEnemy)}: DistanceProfileが未設定です。Approach・Bark・Roamは無効になります。");
        }
        else
        {
            var move = new ApproachBehaviour(_distanceProfile, _services);
            move.Init(initCtx);
            _runner.Register(move);

            var roam = new RoamBehaviour(
                _distanceProfile,
                _services,
                dir => _turn?.SetOverrideDirection(dir)
            );
            roam.Init(initCtx);
            _runner.Register(roam);

        }
        var idle = new IdleBehaviour();
        idle.Init(initCtx);
        _runner.Register(idle);
    }

    /// <summary>
    /// Armor登録イベントを発火する
    /// </summary>
    protected void InvokeArmorRegistered()
    {
        OnArmorRegistered?.Invoke(_armor);
    }

    /// <summary>
    /// 鎧破壊時の処理
    /// </summary>
    private void BreakArmor()
    {
        _defenceContext.EnemyType = EnemyDefenceType.Flesh;
        _armor.OnBroken -= BreakArmor;
        InvokeOnArmorBroken();
    }

    /// <summary>
    /// AttackPatternsリストからランダムに1つ選択する
    /// </summary>
    private EnemyAttackPattern SelectPattern()
    {
        if (_data.AttackPatterns == null || _data.AttackPatterns.Count == 0) return null;
        return _data.AttackPatterns[UnityEngine.Random.Range(0, _data.AttackPatterns.Count)];
    }

    /// <summary>
    /// 0〜1の確率値に対してランダム判定を行う
    /// </summary>
    private bool CheckProbability(float probability)
    {
        return UnityEngine.Random.value < probability;
    }

    /// <summary>
    /// KnockbackContext.Power からノックバックレベルを決定する
    /// </summary>
    /// <returns>Hit / Small / Large</returns>
    private KnockbackLevel DetermineKnockbackLevel(float power)
    {
        if (power <= _data.KnockbackHitThreshold) return KnockbackLevel.Hit;
        if (power >= _data.KnockbackLargeThreshold) return KnockbackLevel.Large;
        return KnockbackLevel.Small;
    }

    /// <summary>
    ///  ダメージ後の追加効果を適用する
    /// （ノックバック・感電など）
    /// </summary>
    /// <param name="context"></param>
    protected virtual void ApplyAdditionalEffects(DamageContext context)
    {
        ApplyKnockback(context);

        ApplyElectricShock(context);
    }

    /// <summary>
    /// ノックバックを適用する
    /// </summary>
    protected void ApplyKnockback(DamageContext context)
    {
        if (context.Knockback == null) return;

        KnockbackContext temp = (KnockbackContext)context.Knockback;

        _lastHitDirection = temp.Direction;

        KnockbackLevel knockbackLevel = DetermineKnockbackLevel(temp.Power);

        _conditionController.ApplyCondition(
            new KnockbackCondition(
                temp,
                knockbackLevel,
                _data.KnockbackStunDuration,
                _data.KnockbackDeceleration));
    }

    /// <summary>
    /// 感電抽選を行い、成功時は感電状態を付与する
    /// </summary>
    protected void ApplyElectricShock(DamageContext context)
    {
        if (!CheckProbability(context.ElectricShock.GrantEffectProbability))
        {
            return;
        }

        _conditionController.ApplyCondition(
            new ElectrifiedCondition(context.ElectricShock.DurationEffect,enemyIsBoss: false));

        this.ActivateShockDebuff().Forget();
    }

#if UNITY_EDITOR
    // Attacker取得中の敵の頭上にマーカーを常時表示する
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (_services.AttackerSlot == null) return;
        if (!_services.AttackerSlot.IsAcquired(Id)) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + Vector3.up * 2.5f, 0.2f);
    }

    // デバッグ用にシーンビューで球体を描く
    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;
        var pattern = _data.AttackPatterns?.Count > 0 ? _data.AttackPatterns[0] : null;
        if (pattern == null) return;

        Gizmos.color = Color.red;
        // TODO: Debug用機能なので、優先度低い
        // TODO: 当たり判定の中心がtransform.forwardのためずれてしまう。
        // TODO: 自分が向いている方向を取得して反映しなければいけない
        Gizmos.DrawWireSphere(transform.position + transform.forward * pattern.AttackRange, pattern.AttackRadius);
    }
#endif
}
