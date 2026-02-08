using UnityEngine;

// NOTE:
// モブ敵のの基底クラスとして作成

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
    }

    public override void TakeDamage(DamageContext context)
    {
        base.TakeDamage(context);

        TryApplyElectricShockSkill(context.ElectricShock);
    }

    private EnemyBehaviourRunner _runner;
    private EnemyContext _context;
    private EnemyStateManager _state;

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
            this.ActivateShockDebuff();

            _state.SetDurationTime(electricShock.DurationEffect);

            _state.ChangeState(EnemyState.Shock);
        }
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
