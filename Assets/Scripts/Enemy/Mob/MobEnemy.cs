using Cysharp.Threading.Tasks;
using UnityEngine;

// NOTE:
// モブ敵のの基底クラスとして作成
// ・感電する
// ・鎧を登録できる
// ※鎧が残っているかはEnemyTypeで判定
// ※鎧持ちでも感電する

public class MobEnemy : Enemy
{
    public override void Init(IPlayer player)
    {
        base.Init(player);

        _context = new EnemyContext();
        _runner = new EnemyBehaviourRunner();
        _state = new EnemyStateManager();

        var move = new MoveBehaviour();
        var attack = new MeleeAttackBehaviour();
        var shock = new ShockBehaviour();

        move.Init(this, _data, _playerTransform, _context, _state);
        attack.Init(this, _data, _playerTransform, _context, _state);
        shock.Init(this, _data, _playerTransform, _context, _state);

        _runner.Add(move);
        _runner.Add(attack);
        _runner.Add(shock);

        // 鎧登録　データがなければ裸
        // TODO: 再生成に対応できる場所だろうか？
        if (_armor != null)
        {
            _defenceContext.EnemyType = EnemyType.Armor;
            _armor.Init(this);
            _armor.OnBroken += BreakArmor;
            // TODO: どこかで購読をやめさせなければ→一応BreakArmor内で対応
        }
        else
        {
            _defenceContext.EnemyType = EnemyType.Flesh;
        }
    }

    public override void TakeDamage(DamageContext context)
    {
        // 本当はよくないが、鎧をMobEnemyだけに持たせる都合で
        // TakeDamageを完全にここに持ってくる
        // 要相談
        if (_isDead) { return; }

        // TODO: そもそもCalculateでfloatにしないのはなぜでしょうか？
        int damage = DamageSystem.Calculate(context, _defenceContext);

        // 鎧がダメージを肩代わり
        if (_defenceContext.EnemyType == EnemyType.Armor)
        {
            if (_armor != null) damage = Mathf.FloorToInt(_armor.AbsorbDamageAndReturnExcess(damage));
        }

        //超過ダメージを生身に流す
        _stats.TakeDamage(damage);

        TryApplyElectricShockSkill(context.ElectricShock);
    }

    // Armorの登録
    [SerializeField] private MobArmor _armor;

    private EnemyBehaviourRunner _runner;
    private EnemyContext _context;
    private EnemyStateManager _state;

    // TODO: Overrideしなくても大丈夫なのか？
    private void OnDestroy()
    {
         if(_armor!=null)_armor.OnBroken -= BreakArmor;
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        if (_runner == null) { return; }
        _runner.Tick(deltaTime);
    }

    private void TryApplyElectricShockSkill(ElectricShock electricShock)
    {

        //最低限の感電状態でreturn
        if (this._defenceContext.HasShockDebuff) return;

        if (CheckProbability(electricShock.GrantEffectProbability))
        {
            this.ActivateShockDebuff().Forget();

            _state.SetDurationTime(electricShock.DurationEffect);

            _state.ChangeState(EnemyState.Shock);
        }
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
