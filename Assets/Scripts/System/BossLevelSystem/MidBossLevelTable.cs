using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MidBossLevelTable", menuName = "GameData/Enemy/MidBossLevelTable")]
public class MidBossLevelTable : ScriptableObject
{
    public List<BossLevelEntry> Levels;

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 1; i < Levels.Count; i++)
        {
            if (Levels[i].RequiredPlayerLevel < Levels[i - 1].RequiredPlayerLevel)
            {
                Debug.LogWarning($"{name}: Levels[{i}] の RequiredPlayerLevel が前の要素より小さいです。昇順で並べてください。");
            }
        }

        if (Levels.Count > 0 && Levels[0].RequiredPlayerLevel != 0)
        {
            Debug.LogWarning($"{name}: 先頭要素の RequiredPlayerLevel が0ではありません。想定外のレベルでフォールバックが発生する可能性があります。");
        }
    }
#endif
}

[System.Serializable]
public class BossLevelEntry
{
    [Header("参照プレイヤーレベル")]
    public int RequiredPlayerLevel;
    [Header("中ボスのEnemyData")]
    public EnemyData EnemyData;
}

