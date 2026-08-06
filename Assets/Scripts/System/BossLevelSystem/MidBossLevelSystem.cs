using UnityEngine;

public class MidBossLevelSystem
{
    /// <summary>
    /// プレイヤーレベルに応じて使用するEnemyDataを選択する。
    /// RequiredPlayerLevel以下の中で最もレベルが高いエントリを採用する。
    /// </summary>
    public static EnemyData SelectEnemyData(MidBossLevelTable table, int playerLevel)
    {
        if (table == null || table.Levels == null || table.Levels.Count == 0)
        {
            Debug.LogError("MidBossLevelTable が未設定、または空です");
            return null;
        }

        // どの条件にも一致しない場合のフォールバックとして先頭のEnemyDataを採用する
        EnemyData selected = table.Levels[0].EnemyData;
        // 現在選択中の条件レベル（より高い条件が見つかれば更新する）
        int selectedRequiredPlayerLevel = int.MinValue;

        foreach (var entry in table.Levels)
        {
            if (playerLevel >= entry.RequiredPlayerLevel &&
                               entry.RequiredPlayerLevel > selectedRequiredPlayerLevel)
            {
                selected = entry.EnemyData;
                selectedRequiredPlayerLevel = entry.RequiredPlayerLevel;
            }
        }

        return selected;
    }
}
