using System.Collections.Generic;

/// <summary> CueSheetの名前保管クラス </summary>
public class CueSheetPathHolder
{
    public readonly Dictionary<CueSheetType, string> CueSheetPathDict = new Dictionary<CueSheetType, string>();

    public CueSheetPathHolder()
    {
        CueSheetPathDict.Add(CueSheetType.UI, "UI_SE");
        CueSheetPathDict.Add(CueSheetType.Player, "Player_SE");
        CueSheetPathDict.Add(CueSheetType.Enemy, "Enemy_SE");
        CueSheetPathDict.Add(CueSheetType.Common, "Common_SE");
        CueSheetPathDict.Add(CueSheetType.InGameBGM, "InGame_BGM");
    }
}

public enum CueSheetType
{
    None,
    UI,
    Player,
    Enemy,
    Common,
    InGameBGM,
    Golem,
    Mob
}
