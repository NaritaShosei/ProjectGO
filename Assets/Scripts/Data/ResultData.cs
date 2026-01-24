/// <summary>
/// リザルトデータ構造体
/// </summary>
public readonly struct ResultData
{
    // 概要
    public bool IsCleared { get; }
    public int ClearWaveCount { get; }

    // 戦績
    public int KillCount { get; }
    public int ComboCount { get; }
    public int DamageCount { get; }
    public int TakeDamageCount { get; }
    public int HealingCount { get; }

    // ビルド構成
    public string BuildBalanceText { get; }
    public string SkillListText { get; }
    public string FinalStatsText { get; }

    public ResultData(bool isCleared, int clearWaveCount, int killCount, int comboCount,
        int damageCount, int takeDamageCount, int healingCount,
        string buildBalanceText, string skillListText, string finalStatsText)
    {
        IsCleared = isCleared;
        ClearWaveCount = clearWaveCount;
        KillCount = killCount;
        ComboCount = comboCount;
        DamageCount = damageCount;
        TakeDamageCount = takeDamageCount;
        HealingCount = healingCount;
        BuildBalanceText = buildBalanceText;
        SkillListText = skillListText;
        FinalStatsText = finalStatsText;
    }
}
