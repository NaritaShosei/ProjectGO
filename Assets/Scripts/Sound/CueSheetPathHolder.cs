using System.Collections.Generic;

/// <summary> CueSheetの名前保管クラス </summary>
public class CueSheetPathHolder
{
    public readonly Dictionary<CueSheetType, string> CueSheetPathDict = new Dictionary<CueSheetType, string>();

    public CueSheetPathHolder()
    {
        CueSheetPathDict.Add(CueSheetType.CommonSE, "Common_SE");
        CueSheetPathDict.Add(CueSheetType.InGameBGM, "InGame_BGM");
        CueSheetPathDict.Add(CueSheetType.RaijinSE, "Raijin_SE");
        CueSheetPathDict.Add(CueSheetType.TousinSE, "Tousin_SE");
    }
}

public enum CueSheetType
{
    None,
    CommonSE,
    InGameBGM,
    RaijinSE,
    TousinSE
}
