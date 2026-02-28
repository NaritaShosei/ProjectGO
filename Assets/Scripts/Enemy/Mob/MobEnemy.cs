using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

// NOTE:
// モブ敵のの基底クラスとして作成
// ・感電する
// ・鎧を登録できる
// ※鎧が残っているかはEnemyTypeで判定
// ※鎧持ちでも感電する

public class MobEnemy : Enemy
{
    public override EnemyConditionController ConditionController { get => _conditionController; }    
    public override void Init(IPlayer player)
    {
        base.Init(player);

        _context = new EnemyContext();
        _runner = new EnemyBehaviourRunner(this);
        _state = new EnemyStateContext();

        _conditionController = new EnemyConditionController(this);

        var move = new MoveBehaviour();
        var attack = new MeleeAttackBehaviour();

        move.Init(this, _data, _playerTransform, _context, _state);
        attack.Init(this, _data, _playerTransform, _context, _state);

        _runner.Register(move);
        _runner.Register(attack);

        // 鎧登録　データがなければ裸
        if (_armor != null)
        {
            _defenceContext.EnemyType = EnemyType.Armor;
            _armor.Init(this);
            _armor.OnBroken += BreakArmor;
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

        // -------- 追加効果 --------

        if (context.Knockback != null)
        {
            // Knockback?はそのまま渡せないので。。
            KnockbackContext temp = (KnockbackContext)context.Knockback;
            _conditionController.ApplyCondition(new KnockbackCondition(temp));
        }

        if(CheckProbability(context.ElectricShock.GrantEffectProbability))
        {
            // もちろんボスじゃないのでfalse
            _conditionController.ApplyCondition(
                new ElectrifiedCondition(context.ElectricShock.DurationEffect,　enemyIsBoss: false));

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

    private void OnDestroy()
    {
         if(_armor!=null)_armor.OnBroken -= BreakArmor;
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        if (_runner == null || _conditionController == null) { return; }
        _conditionController.Tick(deltaTime);
        if (_conditionController.BlocksAction) { return; }
        _runner.Tick(deltaTime);
    }

    /// <summary>
    /// 鎧破壊時の処理
    /// </summary>
    private void BreakArmor(IEnemy enemy)
    {
        _defenceContext.EnemyType = EnemyType.Flesh;
        _armor.OnBroken -= BreakArmor;
    }

    // 確率計算メソッド
    // TODO: いろいろなところで使うと思うので、Utilityにできたほうがいいのでは
    private bool CheckProbability(float probability)
    {
        return Random.value < probability;
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
