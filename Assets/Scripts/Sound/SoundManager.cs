using UnityEngine;
using CriWare;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

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

        // Atom Craft側で各キューに設定されたカテゴリへ一括反映する。
        // 再生中の音と、以降に再生する音の両方へ同じ音量が適用される。
        ApplyCategoryVolume(BGMCategoryName, settings.BGMVolume);
        ApplyCategoryVolume(SECategoryName, settings.SEVolume);
        ApplyCategoryVolume(VoiceCategoryName, settings.VoiceVolume);
    }

    /// <summary> BGM音量を設定（Inspectorからの動作確認用） </summary>
    public void SetBGMVolume(float volume) => ApplyCategoryVolume(BGMCategoryName, volume);

    /// <summary> SE音量を設定（Inspectorからの動作確認用） </summary>
    public void SetSEVolume(float volume) => ApplyCategoryVolume(SECategoryName, volume);

    /// <summary> Voice音量を設定（Inspectorからの動作確認用） </summary>
    public void SetVoiceVolume(float volume) => ApplyCategoryVolume(VoiceCategoryName, volume);

    public void SetBGMFadeDurations(float fadeInSeconds, float fadeOutSeconds)
    {
        if (_bgmSource == null) return;
        _bgmFadeOutSeconds = Mathf.Clamp(fadeOutSeconds, 0f, 3600f);
        _bgmSource.player.SetFadeInTime(Mathf.RoundToInt(Mathf.Clamp(fadeInSeconds, 0f, 3600f) * 1000f));
        _bgmSource.player.SetFadeOutTime(Mathf.RoundToInt(_bgmFadeOutSeconds * 1000f));
    }

    // ── BGM ──────────────────────────────────────────────

    /// <summary> BGM再生 </summary>
    /// <param name="cueName">再生するBGMのキュー名</param>
    /// <param name="sheetType">再生するBGMのシートの種類</param>
    public void PlayBGM(string cueName, CueSheetType sheetType = CueSheetType.None)
    {
        if (_bgmSource == null || string.IsNullOrWhiteSpace(cueName)) return;
        string sheet = sheetType == CueSheetType.None
            ? _defaultBGMCueSheet
            : _cueSheetPathHolder.CueSheetPathDict[sheetType];
        // CRIのstatus反映前に再要求されても、同じ曲は停止・再生し直さない。
        // ロード待ち・フェードイン・一時停止中も同じ再生要求を維持する。
        if (_requestedBGMSheet == sheet && _requestedBGMCue == cueName) return;

        StopBGM();
        _requestedBGMSheet = sheet;
        _requestedBGMCue = cueName;
        _bgmSource.cueSheet = sheet;
        _bgmSource.cueName = cueName;
        PlayBGMWhenReadyAsync(_bgmRequestVersion, sheet, cueName).Forget();
    }

    /// <summary> BGM停止 </summary>
    public void StopBGM()
    {
        ++_bgmRequestVersion;
        _requestedBGMSheet = null;
        _requestedBGMCue = null;
        if (_bgmSource == null) return;
        // フェードアウト中にStopを再発行するとCRIが即時停止するため、一度だけ呼ぶ。
        if (_bgmStopping) return;
        // 何も鳴っていない状態からのStopでは、次の再生を待たせる必要がない。
        _bgmStopping = _bgmSource.status == CriAtomSource.Status.Playing ||
            _bgmSource.status == CriAtomSource.Status.Prep;
        _bgmSource.Stop();
        _bgmSource.Pause(false);
    }

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
    private int _bgmRequestVersion;
    private string _requestedBGMSheet;
    private string _requestedBGMCue;
    private bool _bgmStopping;
    private float _bgmFadeOutSeconds;

    // 通常SE用のソースを管理するDictionary
    private Dictionary<GameObject, List<CriAtomSource>> _seSourcesDict
        = new Dictionary<GameObject, List<CriAtomSource>>();

    // ループSE用のソースを管理するDictionary（GameObject → (cueName → Source)）
    private Dictionary<GameObject, Dictionary<string, CriAtomSource>> _loopSourcesDict
        = new Dictionary<GameObject, Dictionary<string, CriAtomSource>>();

    private async UniTask PlayBGMWhenReadyAsync(int version, string sheet, string cue)
    {
        if (_bgmStopping)
        {
            // statusやIsFading()はフェードアウトの余韻が終わるより先に変化することがあり、
            // ポーリングで判定すると新旧のBGMが重なって二重に聞こえる。設定した秒数を確実に待つ。
            await UniTask.Delay(Mathf.RoundToInt(_bgmFadeOutSeconds * 1000f), ignoreTimeScale: true);
            _bgmStopping = false;
        }

        while (_bgmSource != null && version == _bgmRequestVersion)
        {
            var acb = CriAtom.GetAcb(sheet);
            if (acb != null)
            {
                if (!acb.GetCueInfo(cue, out _))
                {
                    _requestedBGMSheet = null;
                    _requestedBGMCue = null;
                    Debug.LogWarning($"[SoundManager] BGMキューが見つかりません: {sheet}/{cue}");
                    return;
                }
                // CriAtomSourceはStatus.Stopのときだけloopをプレーヤへ反映する。
                // フェーダ使用時も、次に再生するBGMへ確実にループを指定する。
                _bgmSource.loop = true;
                _bgmSource.player.Loop(true);
                _bgmSource.Play();
                return;
            }
            // ロード画面のTime.timeScale=0でも待機を進める。
            await UniTask.Delay(100, ignoreTimeScale: true);
        }
    }

    /// <summary> 初期化 </summary>
    private void Initialize(GameObject bgmPlayer)
    {
        // BGM用ソースの設定
        if (!bgmPlayer.TryGetComponent<CriAtomSource>(out _bgmSource))
            _bgmSource = bgmPlayer.AddComponent<CriAtomSource>();

        _bgmSource.cueSheet = _defaultBGMCueSheet;
        _bgmSource.playOnStart = false;
        _bgmSource.loop = true;
        _bgmSource.use3dPositioning = false;
        // CRI側でフェードするため、Time.timeScaleやカテゴリ音量の変更に依存しない。
        _bgmSource.player.AttachFader();
        SetBGMFadeDurations(1f, 1f);
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
        // ACFの取り込み漏れやカテゴリ名の不一致を実行時に検出する。
        if (!CriAtomExAcf.GetCategoryInfoByName(categoryName, out _))
        {
            Debug.LogWarning($"[SoundManager] CRIカテゴリが見つかりません: {categoryName}");
            return;
        }

        CriAtomExCategory.SetVolume(categoryName, Mathf.Clamp01(volume));
    }
}
