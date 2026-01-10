using UnityEngine;

public class TestBoss : BossEnemy
{
    [SerializeField] private EnemyArmer _armer;
    [SerializeField] private BossCore _core;

    private void Start()
    {
        // TODO:雑にTransform取得
        _bossPhaseController.Init(FindAnyObjectByType<Player>().transform);
    }

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

        BreakCore();

        if (_bossPhaseController.IsPhaseEnd)
        {
            // 最低限：死亡
            OnDeath();
        }
    }

    private void BreakCore()
    {
        _core.gameObject.SetActive(false);
    }

    protected override void OnDeath()
    {
        Destroy(gameObject);
    }

}
