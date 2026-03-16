using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

// NOTE:
// モブ敵のの基底クラスとして作成
// ・感電する
// ・鎧を登録できる
// ※鎧が残っているかはEnemyTypeで判定
// ※鎧持ちでも感電する

public class MobEnemy : Enemy
{
    public override EnemyConditionController ConditionController { get => _conditionController; }

    // Armor登録を外部（UI等）に通知するイベント
    // 購読者はIArmorHealth越しにHP変化・破壊を受け取る
    public event Action<IArmorHealth> OnArmorRegistered;

    public override void Init(IPlayer player)
    {
        base.Init(player);

        _context = new EnemyContext();
        _runner = new EnemyBehaviourRunner(this);
        _state = new EnemyStateContext();

        _conditionController = new EnemyConditionController(this);

        // TurnProfileが未設定の場合は警告を出してTurnを登録しない
        if (_turnProfile == null)
        {
            Debug.LogWarning($"{nameof(MobEnemy)}: TurnProfileが未設定です。Turnは無効になります。");
        }
        else
        {
            _turn = new TurnBehaviour(_turnProfile);
            _turn.Init(this, _data, _playerTransform, _context, _enemyAnimator, _state);
            _runner.RegisterTurn(_turn);
        }

        // AttackerSlotが未設定の場合は警告を出してAttackを登録しない
        if (_attackerSlot == null)
        {
            Debug.LogWarning($"{nameof(MobEnemy)}: AttackerSlotが未注入です。Attackは無効になります。");
        }
        else
        {
            _attack = new MeleeAttackBehaviour(_attackerSlot);
            _attack.Init(this, _data, _playerTransform, _context, _enemyAnimator, _animator, _state);
            _runner.Register(_attack);

            // BarkをattackerSlotブロック内に移動（nullチェック済みの範囲で登録）
            // distanceProfileがない場合はBarkも登録しない
            if (_distanceProfile != null)
            {
                _bark = new BarkBehaviour(_attackerSlot, _data.BarkChance);
                _bark.Init(this, _data, _playerTransform, _context, _enemyAnimator, _animator, _state);
                _runner.Register(_bark);
            }
        }

        // DistanceProfileが未設定の場合は警告を出してMove・Bark・Roamを登録しない
        if (_distanceProfile == null)
        {
            Debug.LogWarning($"{nameof(MobEnemy)}: DistanceProfileが未設定です。Move・Bark・Roamは無効になります。");
        }
        else
        {
            var move = new MoveBehaviour(
                _distanceProfile,
                _attackerSlot,       // 追加
                _separationService,
                _wallAvoidanceService,
                _spatialHashGrid
            );
            move.Init(this, _data, _playerTransform, _context, _enemyAnimator,_state);
            _runner.Register(move);

            // BarkはattackerSlotブロックへ移動したためここから削除

            var roam = new RoamBehaviour(
                _distanceProfile,
                _attackerSlot,
                _separationService,
                _wallAvoidanceService,
                _spatialHashGrid,
                // Roam中の移動方向をTurnBehaviourに通知する
                dir => _turn?.SetOverrideDirection(dir)
            );
            roam.Init(this, _data, _playerTransform, _context, _enemyAnimator, _state);
            _runner.Register(roam);
        }

        // 鎧登録　データがなければ裸
        if (_armor != null)
        {
            _defenceContext.EnemyType = EnemyType.Armor;
            _armor.Init(this);
            _armor.OnBroken += BreakArmor;
            // Init()後に発火することで購読者がOnHealthChangedを安全に受け取れる
            OnArmorRegistered?.Invoke(_armor);
        }
        else
        {
            _defenceContext.EnemyType = EnemyType.Flesh;
        }
    }

    public override void TakeDamage(DamageContext context)
    {
        if (_isDead) { return; }

        int damage = DamageSystem.Calculate(context, _defenceContext);

        bool armorWasAlive = _defenceContext.EnemyType == EnemyType.Armor;

        // 鎧がダメージを肩代わり
        if (_defenceContext.EnemyType == EnemyType.Armor)
        {
            if (_armor != null) damage = Mathf.FloorToInt(_armor.AbsorbDamageAndReturnExcess(damage));
        }

        //超過ダメージを生身に流す
        _stats.TakeDamage(damage);

        bool isKill = _stats.CurrentHealth <= 0;
        bool isArmorBreak = armorWasAlive && _defenceContext.EnemyType == EnemyType.Flesh;
        bool isWeakPoint = !armorWasAlive && _defenceContext.EnemyType == EnemyType.Flesh;

        // -------- HitResult通知 --------
        context.OnHitResult?.Invoke(
            new HitResult
            {
                IsKill = isKill,
                IsArmorBreak = isArmorBreak,
                IsWeakPoint = isWeakPoint
            });

        InvokeOnDamageDealt(damage, isWeakPoint, context.IsCritical);

        // -------- 追加効果 --------

        if (context.Knockback != null)
        {
            // Knockback?はそのまま渡せないので。。
            KnockbackContext temp = (KnockbackContext)context.Knockback;
            _conditionController.ApplyCondition(new KnockbackCondition(temp));
        }

        if (CheckProbability(context.ElectricShock.GrantEffectProbability))
        {
            // もちろんボスじゃないのでfalse
            _conditionController.ApplyCondition(
                new ElectrifiedCondition(context.ElectricShock.DurationEffect, enemyIsBoss: false));

            this.ActivateShockDebuff().Forget();
        }
    }

    public override void OnConditionInterrupt()
    {
        _runner.ForceExitAction();
    }

    // Armorの登録
    [SerializeField] private MobArmor _armor;

    private EnemyBehaviourRunner _runner;
    private EnemyContext _context;
    private EnemyStateContext _state;
    private EnemyConditionController _conditionController;
    private MeleeAttackBehaviour _attack;
    private TurnBehaviour _turn;
    private BarkBehaviour _bark;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_armor != null) _armor.OnBroken -= BreakArmor;

        // BarkBehaviourのイベント購読を解除する
        _bark?.Dispose();
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        if (_runner == null || _conditionController == null) { return; }
        _conditionController.Tick(deltaTime);
        if (_conditionController.BlocksAction) { return; }
        _runner.Tick(deltaTime);
    }

    /// <summary>
    /// 死亡時のクリーンアップ
    /// _isDead = true後はUpdateが止まりRunnerのTickが呼ばれなくなるため
    /// ここで明示的にBehaviourを終了させてスロットを解放する
    /// </summary>
    protected override void OnDeathInternal()
    {
        _runner?.ForceExitAction();

        // 死亡時にスロットを解放する
        _attack?.ReleaseSlot();

        _enemyAnimator?.SetDead();

        base.OnDeathInternal();
    }

    /// <summary>
    /// 鎧破壊時の処理
    /// </summary>
    private void BreakArmor()
    {
        _defenceContext.EnemyType = EnemyType.Flesh;
        _armor.OnBroken -= BreakArmor;
    }

    // 確率計算メソッド
    // TODO: いろいろなところで使うと思うので、Utilityにできたほうがいいのでは
    private bool CheckProbability(float probability)
    {
        return UnityEngine.Random.value < probability;
    }

#if UNITY_EDITOR
    // デバッグ用にシーンビューで球体を描く
    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;

        Gizmos.color = Color.red;
        // TODO: Debug用機能なので、優先度低い
        // TODO: 当たり判定の中心がtransform.forwardのためずれてしまう。
        // TODO: 自分が向いている方向を取得して反映しなければいけない
        Gizmos.DrawWireSphere(transform.position + transform.forward * _data.AttackRange, _data.AttackRadius);
    }
#endif
}
