using System;
using BossEnemy.Enum;
using UnityEngine;

/// <summary>ボスの姿勢（体制）ごとのFOVズーム設定。</summary>
[Serializable]
public class BossPostureZoom
{
    [Tooltip("対象の姿勢")]
    public PostureType Posture = PostureType.Standing;

    [Tooltip("基準FOVに対する倍率。1で等倍、1未満でズームイン、1超でズームアウト")]
    public float ZoomMultiplier = 1f;

    [Tooltip("この倍率へ到達するまでの時間（秒）")]
    public float Duration = 0.3f;
}
