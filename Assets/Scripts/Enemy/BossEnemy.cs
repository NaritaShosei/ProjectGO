// NOTE:
// BossEnemy は「ボス用の基盤クラス」
// ・フェーズという概念
// ・即死しない死亡処理
// ・ダメージ可否の制御
// だけを定義する
// 実際の行動・攻撃・演出は派生クラスで実装する

public abstract class BossEnemy : Enemy
{
    public int CurrentPhase => _currentPhase;

    public override void TakeDamage(AttackContext context)
    {
        // 派生側で「今ダメージが通るか？」を判断させる
        if (!CanTakeDamage(context)) { return; }

        _currentHP -= context.Damage;

        // ボスはHP0でも即死しない
        if (_currentHP <= 0f)
        {
            OnBossHPZero();
        }
    }

    private int _currentPhase = 1;

    protected override void Awake()
    {
        base.Awake();
        _currentPhase = 1;
    }

    protected virtual bool CanTakeDamage(AttackContext context)
    {
        // デフォルトは通る
        return true;
    }

    protected virtual void OnBossHPZero()
    {
        // 例：フェーズ遷移、ダウン、形態変化
        // Destroy はしない
    }

    protected void ChangePhase(int nextPhase)
    {
        if (_currentPhase == nextPhase) return;

        _currentPhase = nextPhase;
        OnPhaseChanged(nextPhase);
    }

    protected virtual void OnPhaseChanged(int phase)
    {
    }

    protected override void OnDeath()
    {
        // 最終フェーズ専用の死亡演出用
        // オブジェクトの非有効化などはここで行う想定
    }
}
