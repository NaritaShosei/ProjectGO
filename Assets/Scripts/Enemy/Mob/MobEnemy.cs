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

        // TODO: 感電している場合はreturn

        // TODO: 以下の内容を別途メソッドにしたほうが見やすい。
        // TODO: 必ずcontextを参照してで感電する確率を計算させる
        // TODO: EnemyDeffenceContextのhasShockDebuffをtrue
        // TODO: _stateを変更する前にShockのremainTimeを書き換えておく。
        _state.ChangeState(EnemyState.Shock);
    }


    private EnemyBehaviourRunner _runner;
    private EnemyContext _context;
    private EnemyStateManager _state;

    protected override void UpdateEnemy(float deltaTime)
    {
        if (_runner == null) { return; }
        _runner.Tick(deltaTime);
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
