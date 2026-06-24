using UnityEngine;

public static class Sound
{
    // ── BGM ──────────────────────────────────────────────

    public static void PlayBGM(
        string cueName,
        CueSheetType sheet = CueSheetType.None)
    {
        Resolve()?.PlayBGM(cueName, sheet);
    }

    public static void StopBGM()
    {
        Resolve()?.StopBGM();
    }

    public static void PauseBGM()
    {
        Resolve()?.PauseBGM();
    }

    public static void ResumeBGM()
    {
        Resolve()?.ResumeBGM();
    }

    // ── SE ───────────────────────────────────────────────

    public static void PlaySE(
        GameObject obj,
        string cueName,
        CueSheetType sheet)
    {
        Resolve()?.PlaySE(obj, cueName, sheet);
    }

    public static void PlayLoopSE(
        GameObject obj,
        string cueName,
        CueSheetType sheet)
    {
        Resolve()?.PlayLoopSE(obj, cueName, sheet);
    }

    public static void StopSE(GameObject obj)
    {
        Resolve()?.StopSE(obj);
    }

    public static void PauseSE(GameObject obj)
    {
        Resolve()?.PauseSE(obj);
    }

    public static void ResumeSE(GameObject obj)
    {
        Resolve()?.ResumeSE(obj);
    }

    public static void StopLoopSE(
        GameObject obj,
        string cueName = null)
    {
        Resolve()?.StopLoopSE(obj, cueName);
    }

    // ── 内部 ─────────────────────────────────────────────

    private static SoundManager Resolve()
    {
        ServiceLocator.TryGet(out SoundManager manager);
        return manager;
    }
}
