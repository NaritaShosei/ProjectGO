using System;
using UnityEngine;

/// <summary>
/// ボス用の基盤クラス
/// ・フェーズという概念
/// ・即死しない死亡処理
/// ・ダメージ可否の制御
/// だけを定義する
/// 実際の行動・攻撃・演出は派生クラスで実装する
/// </summary>
public abstract class BossEnemy : Enemy
{
    public override bool IsBoss => true;

    /// <summary>
    /// フェーズ移行時に発火するイベント
    /// UI・カメラ演出などがフェーズ変化を受け取るために使用する想定
    /// </summary>
    public event Action OnPhaseChange;

    /// <summary>
    /// CanTakeDamageがtrueの場合のみダメージを適用する
    /// </summary>
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
                IsArmorBreak = false,
                IsWeakPoint = _defenceContext.EnemyType == EnemyType.Flesh
            });
    }

    [SerializeField] private protected BossActionPhaseController _bossPhaseController;

    protected override void Awake()
    {
        _stats = new EnemyStats(_data);

        _stats.OnHealthZero += OnBossHPZero;

        _stats.OnDead += OnDeath;
    }

    /// <summary>
    /// 派生クラスでダメージの通過条件を定義する
    /// デフォルトは常に通る
    /// </summary>
    protected virtual bool CanTakeDamage(DamageContext context)
    {
        return true;
    }

    /// <summary>
    /// HPがゼロになったときに呼ばれる
    /// 派生クラスでフェーズ遷移・ダウン・形態変化などを実装する
    /// </summary>
    protected virtual void OnBossHPZero()
    {
        PhaseChange();
    }

    /// <summary>
    /// 最終フェーズ専用の死亡演出用オーバーライドポイント
    /// オブジェクトの非有効化などは派生クラスで行う想定
    /// </summary>
    protected override void OnDeathInternal()
    {
        base.OnDeathInternal();
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("フェーズ変更");
#endif
    }

}
