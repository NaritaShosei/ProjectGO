using System.Collections.Generic;

public class CueSheetPathRegistry
{
    public readonly Dictionary<CueSheetType, string> CueSheetPathDict = new Dictionary<CueSheetType, string>();

    public CueSheetPathRegistry()
    {
        CueSheetPathDict.Add(CueSheetType.CommonSE, "Common_SE");
        CueSheetPathDict.Add(CueSheetType.InGameBGM, "InGame_BGM");
        CueSheetPathDict.Add(CueSheetType.RaijinSE, "Raijin_SE");
        CueSheetPathDict.Add(CueSheetType.TousinSE, "Toujin_SE");
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
