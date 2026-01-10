using System;
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
    public event Action OnPhaseChange;

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

    [SerializeField] protected private BossPhaseController _bossPhaseController;

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

    protected override void OnDeath()
    {
        // 最終フェーズ専用の死亡演出用
        // オブジェクトの非有効化などはここで行う想定
    }

    /// <summary>
    /// フェーズが終了していないときのみ次のフェーズに移行する
    /// </summary>
    protected virtual void PhaseChange()
    {
        if (!_bossPhaseController.IsPhaseEnd)
        {
            _bossPhaseController.SetPhase();
            OnPhaseChange?.Invoke();
        }
    }
}
