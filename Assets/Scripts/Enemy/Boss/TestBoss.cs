using UnityEngine;

public class TestBoss : BossEnemy
{
    [SerializeField] private EnemyArmer _armer;
    [SerializeField] private BossCore _core;

    protected override void UpdateEnemy(float deltaTime)
    {
        if (_bossPhaseController.IsPhaseEnd) { return; }

        _bossPhaseController.Tick();
    }

    protected override bool CanTakeDamage(AttackContext context)
    {
        // 核が出てない間は本体は無敵
        return _armer.IsBroken;
    }

    protected override void OnBossHPZero()
    {
        PhaseChange();

        // 最低限：死亡
        OnDeath();
    }

    protected override void OnDeath()
    {
        Destroy(gameObject);
    }

}
