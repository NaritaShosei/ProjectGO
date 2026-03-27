using UnityEngine;

/// <summary>
/// テスト用ボスの具象クラス
/// EnemyArmerが破壊されるまで本体は無敵、破壊後にフェーズ遷移する
/// </summary>
public class TestBoss : BossEnemy
{
    [SerializeField] private EnemyArmer _armer;
    [SerializeField] private BossCore _core;

    private void Start()
    {
        // TODO:雑にTransform取得
        _bossPhaseController.Init(_playerTransform);
    }

    /// <summary>
    /// フェーズが終了するまでBossActionPhaseControllerを毎フレーム更新する
    /// </summary>
    protected override void UpdateEnemy(float deltaTime)
    {
        if (_bossPhaseController.IsPhaseEnd) { return; }

        _bossPhaseController.Tick();
    }

    /// <summary>
    /// EnemyArmerが破壊済みの場合のみダメージを通す
    /// </summary>
    protected override bool CanTakeDamage(DamageContext context)
    {
        // 核が出てない間は本体は無敵
        return _armer.IsBroken;
    }

    /// <summary>
    /// HPゼロ時にBossCoreを非表示にし、最終フェーズなら死亡処理を実行する
    /// </summary>
    protected override void OnBossHPZero()
    {
        base.OnBossHPZero();

        BreakCore();

        if (_bossPhaseController.IsPhaseEnd)
        {
            // 最低限：死亡
            OnDeath();
        }
    }

    public override void OnConditionInterrupt() { }

    /// <summary>
    /// 最終フェーズ専用の死亡演出用オーバーライドポイント
    /// </summary>
    protected override void OnDeathInternal()
    {
        base.OnDeathInternal();
    }

    /// <summary>
    /// BossCoreを非表示にする
    /// </summary>
    private void BreakCore()
    {
        _core.gameObject.SetActive(false);
    }
}
