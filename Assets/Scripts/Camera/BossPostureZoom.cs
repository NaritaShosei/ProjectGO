using System;
using BossEnemy.Enum;
using UnityEngine;

/// <summary>ボスの姿勢（体制）ごとのFOVズーム設定。</summary>
[Serializable]
public class BossPostureZoom
{
    [Tooltip("対象の姿勢")]
    public PostureType Posture = PostureType.Standing;

    [Tooltip("この姿勢のときのFOV倍率。1で等倍、1未満でズームイン（寄る）、1超でズームアウト（引く）。\n小さくすると：ボスに寄る\n大きくすると：引いて広く見せる")]
    public float ZoomMultiplier = 1f;

    [Tooltip("この倍率へ到達するまでの時間（秒）。\n増やすと：ゆっくり寄る／引く\n減らすと：素早く切り替わる")]
    public float Duration = 0.3f;
}
