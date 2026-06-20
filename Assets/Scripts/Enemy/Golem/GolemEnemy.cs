using UnityEngine;
using System;

/// <summary>
/// ゴーレム専用Enemy
/// 鎧破壊時のダウン・復帰・威嚇行動を持つ
/// </summary>
public class GolemEnemy : Enemy, IFormationParticipant
{
    public int EnemyId => GetInstanceID();

    public float CombatPower =>
        _data != null ? _data.CombatPower : 0f;

    public int FormationSlotCost => 1;

    public bool IsInAttackCooldown =>
        _context != null &&
        _context.AttackCooldownRemaining > 0f;

    public override IEnemyConditionController ConditionController
    => _conditionController;

    public event Action<IArmorHealth> OnArmorRegistered;

    /// <summary>
    /// ゴーレムの初期化
    /// Behaviour・Condition・Armorを生成して登録する
    /// </summary>
    public override void Init()
    {
        _context = new EnemyRuntimeContext();
        _runner = new EnemyBehaviourRunner(this);
        _state = new EnemyStateContext();

        _conditionController = new EnemyConditionController(this);
        _blinkEffect = new BlinkEffect(_bodyRenderer, _blinkSpeed);
        OnArmorBroken += HandleArmorBroken;

        var initCtx = new BehaviourInitContext(this, _data, _playerTransform, _context, _enemyAnimator, _state);

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
            _attack = new MeleeAttackBehaviour(_services, _animator, _distanceProfile, _attackCooldownOverride);
            _attack.Init(initCtx);
            _attack.OnAttackFinished += HandlePostAttack;
            _runner.Register(_attack);

            // スポーン時にスロット取得を試みる
            // 満杯の場合は OnSlotReleased イベントで再試行される
            _services.AttackerSlot.TryAcquire(Id, 1);

            // BarkをattackerSlotブロック内に移動（nullチェック済みの範囲で登録）
            // distanceProfileがない場合はBarkも登録しない
            if (_distanceProfile != null)
            {
                _bark = new BarkBehaviour(
                        _distanceProfile,
                        _services,
                        _data.BarkChance,true);

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

        }

        // 鎧登録　データがなければ裸
        if (_armor != null)
        {
            _defenceContext.EnemyType = EnemyDefenceType.Armor;
            _armor.Init(this);
            _armor.OnBroken += BreakArmor;
            // Init()後に発火することで購読者がOnHealthChangedを安全に受け取れる
            OnArmorRegistered?.Invoke(_armor);
        }
        else
        {
            _defenceContext.EnemyType = EnemyDefenceType.Flesh;
        }
    }

    /// <summary>
    /// ゴーレム専用ダメージ処理
    ///
    /// 通常時
    /// ・鎧へダメージ
    /// Down中
    /// ・本体HPへダメージ
    /// </summary>
    public override void TakeDamage(DamageContext context)
    {
        bool isDown = ConditionController.HasCondition(ConditionType.Down);

        if (_isDead)
        {
            return;
        }

        int damage = DamageSystem.CalculateDamage(context, _defenceContext);

        int showDamage = damage;

        if (!isDown && _defenceContext.EnemyType == EnemyDefenceType.Armor && _armor != null)
        {
            bool armorWasAlive =
            _defenceContext.EnemyType == EnemyDefenceType.Armor;

            _armor.AbsorbDamageAndReturnExcess(damage);

            bool isArmorBreak =
            armorWasAlive && _defenceContext.EnemyType == EnemyDefenceType.Flesh;

            bool isWeak = context.PlayerMode == PlayerMode.Warrior;

            InvokeOnDamageDealt(
                showDamage,
                isWeak,
                context.IsCritical);

            InvokeOnHitEffect(new HitEffectContext
            {
                Position = transform.position,
                PlayerMode = context.PlayerMode,
                IsArmorHit = !isArmorBreak,
                IsArmorBreak = isArmorBreak
            });

            context.OnHitResult?.Invoke(
                new HitResult
                {
                    IsKill = false,
                    IsArmorBreak = isArmorBreak,
                    IsWeakPoint = isWeak,
                    IsArmorHit = !isArmorBreak,
                });

            if (!isArmorBreak)
            {
                InvokeOnDamaged();
            }
            return;
        }

        // Down中、または鎧なし/生身状態は本体HPへ
        bool isWeakPoint = context.PlayerMode == PlayerMode.Thunder;

        InvokeOnDamageDealt(showDamage, isWeakPoint, context.IsCritical);

        _stats.TakeDamage(damage);

        bool willKill = _stats.CurrentHealth <= 0;


        context.OnHitResult?.Invoke(
            new HitResult
            {
                IsKill = willKill,
                IsArmorBreak = false,
                IsWeakPoint = isWeakPoint,
                IsArmorHit = false,
            });

        if (!willKill)
        {
            InvokeOnDamaged();
        }

        if (willKill)
        {
            _stats.Kill();
        }
    }

    /// <summary>
    /// Conditionによる行動中断通知
    /// 現在実行中のBehaviourを強制終了する
    /// </summary>
    public override void OnConditionInterrupt()
    {
        _runner.ForceExitAction();
    }

    /// <summary>
    /// ダウン終了後に鎧を復元する
    /// </summary>
    public void RecoverArmor()
    {
        _blinkEffect.StopBlink();

        if (_armor == null)
        {
            Debug.LogWarning("Armor is null.");
            return;
        }

        _armor.Restore();
        RebindArmor();
        _defenceContext.EnemyType = EnemyDefenceType.Armor;
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

    /// <summary>
    /// 鎧イベントを再購読する
    /// Armor復元後に呼ぶ
    /// </summary>
    public void RebindArmor()
    {
        if (_armor == null)
            return;

        _armor.OnBroken -= BreakArmor;
        _armor.OnBroken += BreakArmor;
    }

    [Header("Down Settings")]
    [SerializeField, Tooltip("鎧破壊後にダウン状態を維持する時間（秒）")]
    private float _downDuration = 5f;

    [Header("Blink Effect")]

    [SerializeField, Tooltip("鎧破壊中に点滅させるRenderer")]
    private Renderer[] _bodyRenderer;

    [SerializeField, Tooltip("点滅速度")]
    private int _blinkSpeed = 100;

    [Header("Combat Settings")]
    [SerializeField, Tooltip("ゴーレム専用の攻撃クールダウン（0以下ならAttackPattern設定を使用）")]
    private float _attackCooldownOverride = 5f;

    [SerializeField, Range(0f, 1f), Tooltip("攻撃後に威嚇へ移行する確率")] private float _barkChance = 0.5f;

    [SerializeField]
    protected MobArmor _armor;

    private EnemyBehaviourRunner _runner;
    private EnemyRuntimeContext _context;
    private EnemyStateContext _state;
    private EnemyConditionController _conditionController;

    private MeleeAttackBehaviour _attack;
    private TurnBehaviour _turn;
    private BarkBehaviour _bark;

    private BlinkEffect _blinkEffect;

    /// <summary>
    /// 毎フレーム更新
    /// 攻撃CT・Condition・Behaviourを更新する
    /// </summary>
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
    /// 攻撃終了時処理
    /// 確率で威嚇Behaviourを強制実行する
    /// </summary>
    private void HandlePostAttack()
    {
        if (_bark == null)
            return;

        if (UnityEngine.Random.value < _barkChance)
        {
            _runner.ForceBehaviour(_bark);
        }
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

    /// <summary>
    /// ランダムに攻撃パターンを選択する
    /// </summary>
    private EnemyAttackPattern SelectPattern()
    {
        if (_data.AttackPatterns == null ||
            _data.AttackPatterns.Count == 0)
            return null;

        return _data.AttackPatterns[UnityEngine.Random.Range(0, _data.AttackPatterns.Count)];
    }

    /// <summary>
    /// 確率判定
    /// </summary>
    private bool CheckProbability(float probability)
    {
        return UnityEngine.Random.value < probability;
    }

    /// <summary>
    /// 死亡時処理
    /// DeadCondition適用とドロップ抽選を行う
    /// </summary>
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

    /// <summary>
    /// 鎧破壊時処理
    /// 点滅演出開始とDownConditionを付与する
    /// </summary>
    private void HandleArmorBroken(IEnemy enemy)
    {
        _blinkEffect.StartBlink();

        ConditionController.ApplyCondition(
            new DownCondition(_downDuration));
    }

    /// <summary>
    /// 鎧破壊完了処理
    /// 防御タイプをFleshへ変更する
    /// </summary>
    private void BreakArmor()
    {
        _defenceContext.EnemyType = EnemyDefenceType.Flesh;
        _armor.OnBroken -= BreakArmor;
        InvokeOnArmorBroken();
    }

    protected override void OnDestroy()
    {
        OnArmorBroken -= HandleArmorBroken;
        if (_attack != null)
        {
            _attack.OnAttackFinished -= HandlePostAttack;
            _attack?.Dispose();
        }

        if (_armor != null)
        {
            _armor.OnBroken -= BreakArmor;
        }

        _bark?.Dispose();
       

        _blinkEffect?.StopBlink();

        base.OnDestroy();
    }
}
