using UnityEngine;

// NOTE:
// この GoblinEnemy は「基盤用の最小実装」です。
// ・複雑なAI
// ・スキル
// ・状態遷移
// は意図的に入れていません。
// 拡張する場合はこのクラスを参考に派生 or 分離してください。

public class GoblinEnemy : Enemy
{
    public override void Init(IPlayer player)
    {
        base.Init(player);

        _runner = new EnemyBehaviourRunner();

        var move = new MoveBehaviour();
        var attack = new MeleeAttackBehaviour();

        move.Init(this, _data, _playerTransform);
        attack.Init(this, _data, _playerTransform);

        _runner.Add(move);
        _runner.Add(attack);
    }


    private EnemyBehaviourRunner _runner;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        _runner.Tick(deltaTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * _data.AttackRange, _data.AttackRadius);
    }
#endif
}
