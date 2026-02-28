using System;
using UnityEngine;

public class BossCore : MonoBehaviour, IEnemy
{
    public event Action<IEnemy> OnDead;
    public EnemyConditionController ConditionController { get; }
    public Vector3 Position { get => transform.position; }


    public void AddKnockBackForce(Vector3 direction)
    {
        // ノックバックなし
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public void TakeDamage(DamageContext context)
    {
        if (context.PlayerMode != PlayerMode.Thunder)
            return;

        // TODO:雑にボスにダメージを与える橋渡しになっているため、仕様によって変更の余地

        _boss.TakeDamage(new DamageContext
        {
            AttackPower = context.AttackPower * _damageMultiplier,
            PlayerMode = context.PlayerMode
        });
    }
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    public  void OnConditionInterrupt() { }

    public void Init(IPlayer player)
    {
        // プレイヤーの参照は不要
    }

    [SerializeField] private TestBoss _boss;
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private Transform _targetCenter;
}
