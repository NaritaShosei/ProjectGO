using UnityEngine;

public static class Sound
{
    // ── BGM ──────────────────────────────────────────────

    /// <summary> BGM再生 </summary>
    public static void PlayBGM(string cueName, CueSheetType sheet = CueSheetType.None)
        => Resolve()?.PlayBGM(cueName, sheet);

    /// <summary> BGM停止 </summary>
    public static void StopBGM()
        => Resolve()?.StopBGM();

    /// <summary> BGM一時停止 </summary>
    public static void PauseBGM()
        => Resolve()?.PauseBGM();

    /// <summary> BGM再開 </summary>
    public static void ResumeBGM()
        => Resolve()?.ResumeBGM();

    // ── SE 汎用 ───────────────────────────────────────────

    /// <summary> SE再生 </summary>
    public static void PlaySE(GameObject obj, string cueName, CueSheetType sheet)
        => Resolve()?.PlaySE(obj, cueName, sheet);

    /// <summary> SE停止 </summary>
    public static void StopSE(GameObject obj)
        => Resolve()?.StopSE(obj);

    /// <summary> SE一時停止 </summary>
    public static void PauseSE(GameObject obj)
        => Resolve()?.PauseSE(obj);

    /// <summary> SE再開 </summary>
    public static void ResumeSE(GameObject obj)
        => Resolve()?.ResumeSE(obj);

    /// <summary> ループSE再生 </summary>
    public static void PlayLoopSE(GameObject obj, string cueName, CueSheetType sheet)
        => Resolve()?.PlayLoopSE(obj, cueName, sheet);

    /// <summary> ループSE停止 </summary>
    public static void StopLoopSE(GameObject obj, string cueName = null)
        => Resolve()?.StopLoopSE(obj, cueName);


    // ── SE キャラ別ショートカット ──────────────────────────

    /// <summary> CommonSEシートのSE再生 </summary>
    public static void PlayCommonSE(GameObject obj, string cueName)
        => Resolve()?.PlaySE(obj, cueName, CueSheetType.CommonSE);

    /// <summary> 雷神のSE再生 </summary>
    public static void PlayRaijinSE(GameObject obj, string cueName)
        => Resolve()?.PlaySE(obj, cueName, CueSheetType.RaijinSE);

    /// <summary> 雷神のループSE再生 </summary>
    public static void PlayRaijinLoopSE(GameObject obj, string cueName)
        => Resolve()?.PlayLoopSE(obj, cueName, CueSheetType.RaijinSE);

    /// <summary> 闘神のSE再生 </summary>
    public static void PlayTousnSE(GameObject obj, string cueName)
        => Resolve()?.PlaySE(obj, cueName, CueSheetType.TousinSE);

    // ── 内部 ─────────────────────────────────────────────

    /// <summary> ServiceLocatorからSoundManagerを解決 </summary>
    private static SoundManager Resolve()
    {
        ServiceLocator.TryGet(out SoundManager manager);
        return manager;
    }
}
