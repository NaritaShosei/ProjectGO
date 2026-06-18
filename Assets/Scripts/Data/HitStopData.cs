using System;
using UnityEngine;

/// <summary>
/// 攻撃に紐づくヒットストップ設定データ。
/// AttackData から参照される ScriptableObject。
/// </summary>
[Serializable]
public sealed class HitStopData
{
    /// <summary> 基礎停止時間（秒） </summary>
    public float BaseDuration => _baseDuration;

    /// <summary> 停止中のタイムスケール（0 = 完全停止） </summary>
    public float TimeScale => _timeScale;

    /// <summary> ヒットストップ対象グループ </summary>
    public HitStopTargetGroup TargetGroup => _targetGroup;

    /// <summary> 優先順位(数字が低いほど優先度が高い) </summary>
    public int Priority => _priority;

    /// <summary> 弱点ヒット時の倍率 </summary>
    public float WeakPointMultiplier => _weakPointMultiplier;

    /// <summary> 鎧破壊 or 撃破時の倍率 </summary>
    public float BreakOrKillMultiplier => _breakOrKillMultiplier;

    /// <summary> 弱点 + 鎧破壊 or 撃破 同時発生時の倍率 </summary>
    public float WeakAndBreakOrKillMultiplier => _weakAndBreakOrKillMultiplier;

    /// <summary>
    /// 命中結果に応じた最終的なヒットストップ時間を取得する
    /// </summary>
    public float GetDuration(bool isWeakPoint, bool isArmorBreak, bool isKill)
    {
        return _baseDuration * GetMultiplier(isWeakPoint, isArmorBreak, isKill);
    }

    [Header("Basic")]
    [Tooltip("基礎停止時間（秒）")]
    [SerializeField] private float _baseDuration = 0.08f;

    [Tooltip("停止中のタイムスケール（0 = 完全停止）")]
    [SerializeField] private float _timeScale = 0f;

    [Tooltip("停止対象グループ")]
    [SerializeField]
    private HitStopTargetGroup _targetGroup =
        HitStopTargetGroup.Player |
        HitStopTargetGroup.HitEnemy |
        HitStopTargetGroup.Effects;

    [Tooltip("優先順位(数字が低いほど優先度が高い)")]
    [SerializeField] private int _priority = 999;

    [Header("Multiplier")]
    [Tooltip("弱点ヒット時の倍率")]
    [SerializeField] private float _weakPointMultiplier = 1.2f;

    [Tooltip("鎧破壊 or 撃破（弱点外）時の倍率")]
    [SerializeField] private float _breakOrKillMultiplier = 1.2f;

    [Tooltip("弱点 + 鎧破壊 or 弱点 + 撃破 同時発生時の倍率")]
    [SerializeField] private float _weakAndBreakOrKillMultiplier = 1.4f;

    /// <summary>
    /// 命中状況からヒットストップ倍率を取得する
    /// </summary>
    private float GetMultiplier(bool isWeakPoint, bool isArmorBreak, bool isKill)
    {
        bool isBreakOrKill = isArmorBreak || isKill;

        if (isWeakPoint && isBreakOrKill)
        {
            return _weakAndBreakOrKillMultiplier;
        }

        if (isWeakPoint)
        {
            return _weakPointMultiplier;
        }

        if (isBreakOrKill)
        {
            return _breakOrKillMultiplier;
        }

        return 1f;
    }
}

[Flags]
public enum HitStopTargetGroup
{
    None = 0,
    Player = 1 << 0,
    HitEnemy = 1 << 1,  // ヒットした1体のみ
    AllEnemies = 1 << 2,  // 全敵（クリティカル用）
    Effects = 1 << 3,  // 戦闘エフェクト（環境VFX除く）
    Camera = 1 << 4,
}
