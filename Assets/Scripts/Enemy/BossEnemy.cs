using UnityEngine;

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

    [SerializeField] private int _currentPhase = 1;

    protected override void Awake()
    {
        base.Awake();
        _currentPhase = 1;
    }

    protected void ChangePhase(int nextPhase)
    {
        if (_currentPhase == nextPhase) return;

        _currentPhase = nextPhase;
        OnPhaseChanged(nextPhase);
    }

    protected virtual void OnPhaseChanged(int phase)
    {
        // フェーズ切り替え演出・行動変更など
    }

    protected override void OnDeath()
    {
        // ボス専用の死亡演出用フック
        // 今は何もしない
    }

    public override void TakeDamage(AttackContext context)
    {

    }
}
