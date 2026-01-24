/// <summary>
/// リザルトデータ構造体
/// </summary>
public struct ResultData
{
    // 概要
    public bool IsCleared { get; }
    public string ClearWaveCountText { get; }

    // 戦績
    public string KillCountText { get; }
    public string ComboCountText { get; }
    public string DamageCountText { get; }
    public string TakeDamageCountText { get; }
    public string HealingCountText { get; }

    // ビルド構成
    public string BuildBalanceText { get; }
    public string SkillListText { get; }
    public string FinalStatsText { get; }

    public ResultData(bool isCleared, string clearWaveCountText, string killCountText, string comboCountText,
        string damageCountText, string takeDamageCountText, string healingCountText,
        string buildBalanceText, string skillListText, string finalStatsText)
    {
        IsCleared = isCleared;
        ClearWaveCountText = clearWaveCountText;
        KillCountText = killCountText;
        ComboCountText = comboCountText;
        DamageCountText = damageCountText;
        TakeDamageCountText = takeDamageCountText;
        HealingCountText = healingCountText;
        BuildBalanceText = buildBalanceText;
        SkillListText = skillListText;
        FinalStatsText = finalStatsText;
    }
}
