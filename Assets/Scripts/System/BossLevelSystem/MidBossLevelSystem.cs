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

        EnemyData selected = table.Levels[0].EnemyData; // 最低保証（最序盤フォールバック）

        foreach (var entry in table.Levels)
        {
            if (playerLevel >= entry.RequiredPlayerLevel)
                selected = entry.EnemyData;
            else
                break; // Levelsが昇順ソート済み前提
        }

        return selected;
    }
}
