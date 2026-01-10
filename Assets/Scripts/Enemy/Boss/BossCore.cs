using UnityEngine;

public class BossCore : MonoBehaviour, IEnemy
{
    [SerializeField] private TestBoss _boss;
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private Transform _targetCenter;

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

        _boss.TakeDamage(new AttackContext
        {
            Damage = context.Damage * _damageMultiplier,
            PlayerMode = context.PlayerMode
        });
    }
}
