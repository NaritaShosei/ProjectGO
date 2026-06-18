using UnityEngine;


/// <summary>
/// 次のSpawnGroupへ進む条件の種類
/// </summary>
public enum WaveConditionType
{
    TimeElapsed,   // 指定秒数が経過したら次へ
    KillCount,     // このグループの撃破数が指定数に達したら次へ
    AllDefeated,   // このグループの全エネミーが全滅したら次へ
}

/// <summary>
/// 次のウェーブに進む条件のデータクラス
/// </summary>
[System.Serializable]
public class NextWaveConditionData
{
    public WaveConditionType WaveConditionType;

    [Tooltip("TimeElapsed: 経過秒数 / KillCount: 撃破数しきい値")]
    public float Threshold;
}
