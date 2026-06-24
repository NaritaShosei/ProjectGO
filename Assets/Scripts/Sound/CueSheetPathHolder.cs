using System.Collections.Generic;

/// <summary> CueSheetの名前保管クラス </summary>
public class CueSheetPathHolder
{
    public readonly Dictionary<CueSheetType, string> CueSheetPathDict = new Dictionary<CueSheetType, string>();

    public CueSheetPathHolder()
    {
        CueSheetPathDict.Add(CueSheetType.UI, "CueSheet_1");
        CueSheetPathDict.Add(CueSheetType.Player, "CueSheet_1");
        CueSheetPathDict.Add(CueSheetType.Enemy, "CueSheet_1");
        CueSheetPathDict.Add(CueSheetType.Common, "CueSheet_1");
        CueSheetPathDict.Add(CueSheetType.InGameBGM, "CueSheet_0");
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
}
