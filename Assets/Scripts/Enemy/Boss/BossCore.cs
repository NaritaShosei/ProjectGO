using System;
using UnityEngine;

public class BossCore : MonoBehaviour, IEnemy
{
    public event Action<IEnemy> OnDead;

    public void AddKnockBackForce(Vector3 direction)
    {
        // ノックバックなし
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public void TakeDamage(AttackContext context)
    {
        if (context.PlayerMode != PlayerMode.Thunder)
            return;

        // TODO:雑にボスにダメージを与える橋渡しになっているため、仕様によって変更の余地

        _boss.TakeDamage(new AttackContext
        {
            Damage = context.Damage * _damageMultiplier,
            PlayerMode = context.PlayerMode
        });
    }

    [SerializeField] private TestBoss _boss;
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private Transform _targetCenter;
}
