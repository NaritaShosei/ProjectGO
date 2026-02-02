using UnityEngine;

// NOTE:
// この GoblinEnemy は「基盤用の最小実装」です。
// ・複雑なAI
// ・スキル
// ・状態遷移
// は意図的に入れていません。
// 拡張する場合はこのクラスを参考に派生 or 分離してください。

public class StunEnemy : Enemy
{
    public override void Init(IPlayer player)
    {
        base.Init(player);

        _context = new EnemyContext();
        _runner = new EnemyBehaviourRunner();

        var move = new MoveBehaviour();
        var attack = new MeleeAttackBehaviour();

        move.Init(this, _data, _playerTransform, _context);
        attack.Init(this, _data, _playerTransform, _context);

        _runner.Add(move);
        _runner.Add(attack);
    }

    // TODO: ひとまずここにTakeDamage→EnemyContextの橋渡しを記述しておく。
    // TODO: 敵を量産するならここじゃないほうがいい
    public override void TakeDamage(DamageContext context)
    {
        base.TakeDamage(context);

        // TODO: スタン攻撃かDamageContextから判別
        // TODO: スタン攻撃ならEnemyContextにおいてスタン状態に変更
        // TODO: EnemyクラスからEnemyContextへの参照がない
        // TODO: ひとまず一つ下のStunEnemyのほうでoverrideして実装しよう
    }


    private EnemyBehaviourRunner _runner;
    private EnemyContext _context;

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
        // TODO: 当たり判定の中心がtransform.forwardのためずれてしまう。
        // TODO: 自分が向いている方向を取得して反映しなければいけない・
        Gizmos.DrawWireSphere(transform.position + transform.forward * _data.AttackRange, _data.AttackRadius);
    }
#endif
}
