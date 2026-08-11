using UnityEngine;
using CriWare;
using System.Collections.Generic;

public class SoundManager
{
    private const string BGMCategoryName = "BGM";
    private const string SECategoryName = "SE";
    private const string VoiceCategoryName = "Voice";

    /// <summary> コンストラクタ </summary>
    /// <param name="bgmPlayer"> BGM用のPlayerObject </param>
    /// <param name="defaultBGM"> BGMの音源データが入ったCueSheetの名前(あとで再生時に変更可能) </param>
    public SoundManager(GameObject bgmPlayer, string defaultBGM)
    {
        _defaultBGMCueSheet = defaultBGM;
        Initialize(bgmPlayer);

        if (ServiceLocator.TryGet(out GameSettingService settingsService))
        {
            ApplySettings(settingsService.CurrentSettings);
        }
    }

    public void ApplySettings(GameSetting settings)
    {
        if (settings == null) return;

        ApplyCategoryVolume(BGMCategoryName, settings.BGMVolume);
        ApplyCategoryVolume(SECategoryName, settings.SEVolume);
        ApplyCategoryVolume(VoiceCategoryName, settings.VoiceVolume);
    }

    // ── BGM ──────────────────────────────────────────────

    /// <summary> BGM再生 </summary>
    /// <param name="cueName">再生するBGMのキュー名</param>
    /// <param name="sheetType">再生するBGMのシートの種類</param>
    public void PlayBGM(string cueName, CueSheetType sheetType = CueSheetType.None)
    {
        _bgmSource.Stop();

        if (sheetType != CueSheetType.None)
            _bgmSource.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];

        _bgmSource.cueName = cueName;
        _bgmSource.Play();
    }

    /// <summary> BGM停止 </summary>
    public void StopBGM() => _bgmSource.Stop();

    /// <summary> BGM一時停止 </summary>
    public void PauseBGM() => _bgmSource.Pause(true);

    /// <summary> BGM再開 </summary>
    public void ResumeBGM() => _bgmSource.Pause(false);

    // ── SE（通常） ────────────────────────────────────────

    /// <summary> SE再生 </summary>
    /// <param name="seObj">SEを鳴らすオブジェクト</param>
    /// <param name="cueName">再生するSEのキュー名</param>
    /// <param name="sheetType">再生するSEのシートの種類</param>
    public void PlaySE(GameObject seObj, string cueName, CueSheetType sheetType)
    {
        if (!_seSourcesDict.ContainsKey(seObj))
            _seSourcesDict.Add(seObj, new List<CriAtomSource>());

        // 停止中のソースを探して再生
        foreach (var source in _seSourcesDict[seObj])
        {
            if (source.status != CriAtomSource.Status.Playing)
            {
                if (sheetType != CueSheetType.None)
                    source.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];

                source.cueName = cueName;
                source.Play();
                return;
            }
        }

        // 全てのソースが再生中の場合、新しいソースを作成して再生
        var newSource = CreateNewSESource(seObj, sheetType);
        newSource.cueName = cueName;
        newSource.Play();
    }

    /// <summary> SE停止。ループSEも併せて停止する </summary>
    /// <param name="seObj">停止するSEのオブジェクト</param>
    public void StopSE(GameObject seObj)
    {
        if (_seSourcesDict.ContainsKey(seObj))
            foreach (var source in _seSourcesDict[seObj])
                source.Stop();

        // ループSEも止める
        StopLoopSE(seObj);
    }

    /// <summary> SE一時停止。ループSEも併せて一時停止する </summary>
    /// <param name="seObj">一時停止するSEのオブジェクト</param>
    public void PauseSE(GameObject seObj)
    {
        if (_seSourcesDict.ContainsKey(seObj))
            foreach (var source in _seSourcesDict[seObj])
                source.Pause(true);

        if (_loopSourcesDict.ContainsKey(seObj))
            foreach (var source in _loopSourcesDict[seObj].Values)
                source.Pause(true);
    }

    /// <summary> SE再開。ループSEも併せて再開する </summary>
    /// <param name="seObj">再開するSEのオブジェクト</param>
    public void ResumeSE(GameObject seObj)
    {
        if (_seSourcesDict.ContainsKey(seObj))
            foreach (var source in _seSourcesDict[seObj])
                source.Pause(false);

        if (_loopSourcesDict.ContainsKey(seObj))
            foreach (var source in _loopSourcesDict[seObj].Values)
                source.Pause(false);
    }

    // ── SE（ループ） ──────────────────────────────────────

    /// <summary>
    /// ループSEを再生する。
    /// 同じcueNameがすでにループ中の場合は何もしない。
    /// </summary>
    /// <param name="seObj">SEを鳴らすオブジェクト</param>
    /// <param name="cueName">再生するSEのキュー名</param>
    /// <param name="sheetType">再生するSEのシートの種類</param>
    public void PlayLoopSE(GameObject seObj, string cueName, CueSheetType sheetType)
    {
        if (!_loopSourcesDict.ContainsKey(seObj))
            _loopSourcesDict.Add(seObj, new Dictionary<string, CriAtomSource>());

        // すでに再生中なら何もしない
        if (_loopSourcesDict[seObj].ContainsKey(cueName))
        {
            var existing = _loopSourcesDict[seObj][cueName];
            if (existing.status == CriAtomSource.Status.Playing)
                return;

            existing.Stop();
            Object.Destroy(existing);
            _loopSourcesDict[seObj].Remove(cueName);
        }

        // 新しいループ用ソースを作成して再生
        var source = seObj.AddComponent<CriAtomSource>();
        source.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];
        source.cueName = cueName;
        source.loop = true;
        source.Play();

        _loopSourcesDict[seObj][cueName] = source;
    }

    /// <summary>
    /// ループSEを停止する。
    /// cueNameを省略するとseObjに紐づく全ループSEを停止する。
    /// </summary>
    /// <param name="seObj">停止するSEのオブジェクト</param>
    /// <param name="cueName">停止するSEのキュー名。nullの場合は全ループSEを停止する</param>
    public void StopLoopSE(GameObject seObj, string cueName = null)
    {
        if (!_loopSourcesDict.ContainsKey(seObj)) return;

        if (cueName != null)
        {
            // 指定したキュー名のループSEのみ停止
            if (_loopSourcesDict[seObj].TryGetValue(cueName, out var source))
            {
                source.Stop();
                Object.Destroy(source);
                _loopSourcesDict[seObj].Remove(cueName);
                if (_loopSourcesDict[seObj].Count == 0)
                    _loopSourcesDict.Remove(seObj);
            }
        }
        else
        {
            // seObjに紐づく全ループSEを停止
            foreach (var source in _loopSourcesDict[seObj].Values)
            {
                source.Stop();
                Object.Destroy(source);
            }
            _loopSourcesDict.Remove(seObj);
        }
    }

    // ── Private ───────────────────────────────────────────

    // デフォルトのBGM CueSheet名
    private readonly string _defaultBGMCueSheet;

    // CueSheetのパスを管理するクラス
    private CueSheetPathHolder _cueSheetPathHolder = new CueSheetPathHolder();

    // BGM用のソース
    private CriAtomSource _bgmSource;

    // 通常SE用のソースを管理するDictionary
    private Dictionary<GameObject, List<CriAtomSource>> _seSourcesDict
        = new Dictionary<GameObject, List<CriAtomSource>>();

    // ループSE用のソースを管理するDictionary（GameObject → (cueName → Source)）
    private Dictionary<GameObject, Dictionary<string, CriAtomSource>> _loopSourcesDict
        = new Dictionary<GameObject, Dictionary<string, CriAtomSource>>();

    /// <summary> 初期化 </summary>
    private void Initialize(GameObject bgmPlayer)
    {
        // BGM用ソースの設定
        if (!bgmPlayer.TryGetComponent<CriAtomSource>(out _bgmSource))
            _bgmSource = bgmPlayer.AddComponent<CriAtomSource>();

        _bgmSource.cueSheet = _defaultBGMCueSheet;
    }

    /// <summary> 新たに通常SE用のSourceを作る処理 </summary>
    private CriAtomSource CreateNewSESource(GameObject seObj, CueSheetType sheetType)
    {
        var newSource = seObj.AddComponent<CriAtomSource>();
        newSource.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];

        _seSourcesDict[seObj].Add(newSource);

        return newSource;
    }

    private static void ApplyCategoryVolume(string categoryName, float volume)
    {
        if (!CriAtomExAcf.GetCategoryInfoByName(categoryName, out _))
        {
            Debug.LogWarning($"[SoundManager] CRIカテゴリが見つかりません: {categoryName}");
            return;
        }

        CriAtomExCategory.SetVolume(categoryName, Mathf.Clamp01(volume));
    }
}
