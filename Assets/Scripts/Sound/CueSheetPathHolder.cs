using System.Collections.Generic;

/// <summary> CueSheetの名前保管クラス </summary>
public class CueSheetPathHolder
{
    public readonly Dictionary<CueSheetType, string> CueSheetPathDict = new Dictionary<CueSheetType, string>();

    public CueSheetPathHolder()
    {
        CueSheetPathDict.Add(CueSheetType.UI, "UI_SE");
        CueSheetPathDict.Add(CueSheetType.Player, "Player_SE");
        CueSheetPathDict.Add(CueSheetType.Enemy, "Mob_SE");
        CueSheetPathDict.Add(CueSheetType.Mob, "Mob_SE");
        CueSheetPathDict.Add(CueSheetType.Golem, "Golem_SE");
        CueSheetPathDict.Add(CueSheetType.Boss, "Boss_SE");
        CueSheetPathDict.Add(CueSheetType.Common, "Common_SE");
        CueSheetPathDict.Add(CueSheetType.Skill, "Skill_SE");
        CueSheetPathDict.Add(CueSheetType.Environment, "Environment_SE");
        CueSheetPathDict.Add(CueSheetType.BGM, "BGM");
        CueSheetPathDict.Add(CueSheetType.InGameBGM, "BGM");
    }
}

public enum CueSheetType
{
    None,
    UI,
    Player,
    Enemy,
    Mob,
    Golem,
    Boss,
    Common,
    Skill,
    Environment,
    BGM,
    InGameBGM,
}
