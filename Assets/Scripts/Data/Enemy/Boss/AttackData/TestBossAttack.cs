using UnityEngine;

[CreateAssetMenu(fileName = "TestAttack",menuName = "GameData/TestAttack")]

public class TestBossAttack : BossAttackBase
{
    public override void Execute(BossAttackContext context)
    {
        var obj = Instantiate(_obj, context.BossTransform.position, Quaternion.identity);

        obj.Init(_damage);

        var vel = (context.Player.position - context.BossTransform.position).normalized;

        var rb =  obj.gameObject.AddComponent<Rigidbody>();

        rb.linearVelocity = vel * 5;
        rb.useGravity = false;
    }

    [SerializeField] private TestBullet _obj;
}
