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

    public override void TakeDamage(DamageContext context)
    {
        // 派生側で「今ダメージが通るか？」を判断させる
        if (!CanTakeDamage(context)) { return; }

        int damage = DamageSystem.Calculate(context, _defenceContext);

        _stats.TakeDamage(damage);

        bool isKill = _stats.CurrentHealth <= 0;

        context.OnHitResult?.Invoke(
            new HitResult
            {
                IsKill = isKill,
                IsArmorBreak = true,
                IsWeakPoint = true
            });
    }

    [SerializeField] protected private BossActionPhaseController _bossPhaseController;

    protected override void Awake()
    {
        _stats = new EnemyStats(_data);

        _stats.OnHealthZero += OnBossHPZero;

        _stats.OnDead += OnDeath;
    }

    protected virtual bool CanTakeDamage(DamageContext context)
    {
        // デフォルトは通る
        return true;
    }

    protected virtual void OnBossHPZero()
    {
        // 例：フェーズ遷移、ダウン、形態変化
        // Destroy はしない
        PhaseChange();
    }

    protected override void OnDeathInternal()
    {
        base.OnDeathInternal();
        // 最終フェーズ専用の死亡演出用
        // オブジェクトの非有効化などはここで行う想定
    }

    /// <summary>
    /// フェーズが終了していないときのみ次のフェーズに移行する
    /// </summary>
    protected virtual void PhaseChange()
    {
        if (_bossPhaseController.IsPhaseEnd)
        {
            // 最終フェーズ → 本当の死亡
            _stats.Kill();
            return;
        }

        _bossPhaseController.SetPhase();
        OnPhaseChange?.Invoke();

        _data = _bossPhaseController.CurrentPhase.Data;
        _stats.ResetHP(_data.MaxHP);

        Debug.Log("フェーズ変更");
    }

}
