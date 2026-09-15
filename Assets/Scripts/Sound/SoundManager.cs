using UnityEngine;
using CriWare;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class SoundManager
{
    private const string SECategoryName = "SE";
    private const string VoiceCategoryName = "Voice";
    // Play()直後はstatusがまだPlaying/Prepに遷移していないことがあるため、この間は早期終了と判定しない
    private const float VoiceDuckStatusGraceSeconds = 0.2f;
    // キュー長に対して余裕を持たせる秒数
    private const float VoiceDuckDeadlineMarginSeconds = 1f;
    // キュー長が取得できなかった場合の上限秒数
    private const float VoiceDuckFallbackMaxSeconds = 10f;

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

        // BGMはSource自身のvolumeへ、SE・VoiceはAtom Craft側のカテゴリへ反映する。
        // 再生中の音と、以降に再生する音の両方へ同じ音量が適用される。
        _baseBGMVolume = settings.BGMVolume;
        _baseSEVolume = settings.SEVolume;
        ApplyDuckableVolumes();
        ApplyCategoryVolume(VoiceCategoryName, settings.VoiceVolume);
    }

    /// <summary> BGM音量を設定（Inspectorからの動作確認用） </summary>
    public void SetBGMVolume(float volume)
    {
        _baseBGMVolume = volume;
        ApplyDuckableVolumes();
    }

    /// <summary> SE音量を設定（Inspectorからの動作確認用） </summary>
    public void SetSEVolume(float volume)
    {
        _baseSEVolume = volume;
        ApplyDuckableVolumes();
    }

    /// <summary> Voice音量を設定（Inspectorからの動作確認用） </summary>
    public void SetVoiceVolume(float volume) => ApplyCategoryVolume(VoiceCategoryName, volume);

    /// <summary> スヴァナのセリフ再生中に、BGM・SEをどこまで下げるかを設定 </summary>
    /// <param name="bgmDuckRatio">セリフ再生中のBGM音量（元の音量に対する倍率）</param>
    /// <param name="seDuckRatio">セリフ再生中のSE音量（元の音量に対する倍率）</param>
    public void SetVoiceDuckRatios(float bgmDuckRatio, float seDuckRatio)
    {
        _bgmDuckRatio = Mathf.Clamp01(bgmDuckRatio);
        _seDuckRatio = Mathf.Clamp01(seDuckRatio);
        ApplyDuckableVolumes();
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
        // ロード待ち・一時停止中も同じ再生要求を維持する。
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
        var source = GetOrCreateSESource(seObj, sheetType);
        source.cueName = cueName;
        source.Play();
    }

    /// <summary>
    /// スヴァナのセリフ（字幕付きの物語ボイス）を再生する。
    /// 攻撃ボイス等の掛け声とは異なり、再生中はBGM・SEをダッキングする。
    /// </summary>
    /// <param name="voiceOwner">セリフを鳴らすオブジェクト</param>
    /// <param name="cueName">再生するセリフのキュー名</param>
    public void PlayNarrationVoice(GameObject voiceOwner, string cueName)
    {
        var source = GetOrCreateSESource(voiceOwner, CueSheetType.PlayerVoice);
        source.cueName = cueName;
        source.Play();

        ++_activeVoiceDuckCount;
        ApplyDuckableVolumes();
        WatchVoiceDuckEndAsync(source, GetCueLengthSeconds(cueName)).Forget();
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
    /// <param name="use3dPositioning">falseの場合、距離減衰しない2D的な鳴らし方にする</param>
    /// <returns>再生に使ったSource（呼び出し側で個別に音量等を調整したい場合に使う）</returns>
    public CriAtomSource PlayLoopSE(GameObject seObj, string cueName, CueSheetType sheetType, bool use3dPositioning = true)
    {
        if (!_loopSourcesDict.ContainsKey(seObj))
            _loopSourcesDict.Add(seObj, new Dictionary<string, CriAtomSource>());

        // すでに再生中なら何もしない
        if (_loopSourcesDict[seObj].ContainsKey(cueName))
        {
            var existing = _loopSourcesDict[seObj][cueName];
            if (existing.status == CriAtomSource.Status.Playing)
                return existing;

            existing.Stop();
            Object.Destroy(existing);
            _loopSourcesDict[seObj].Remove(cueName);
        }

        // 新しいループ用ソースを作成して再生
        var source = seObj.AddComponent<CriAtomSource>();
        source.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];
        source.cueName = cueName;
        source.loop = true;
        source.use3dPositioning = use3dPositioning;
        if (!use3dPositioning)
            // キュー側が3D設定でも距離減衰しないよう、再生方式を明示する。
            source.player.SetPanType(CriAtomEx.PanType.Pan3d);
        source.Play();

        _loopSourcesDict[seObj][cueName] = source;
        return source;
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

    // スヴァナのセリフ（PlayerVoice）再生中のダッキング
    private float _baseBGMVolume = 1f;
    private float _baseSEVolume = 1f;
    private float _bgmDuckRatio = 1f;
    private float _seDuckRatio = 1f;
    private int _activeVoiceDuckCount;

    // 通常SE用のソースを管理するDictionary
    private Dictionary<GameObject, List<CriAtomSource>> _seSourcesDict
        = new Dictionary<GameObject, List<CriAtomSource>>();

    // ループSE用のソースを管理するDictionary（GameObject → (cueName → Source)）
    private Dictionary<GameObject, Dictionary<string, CriAtomSource>> _loopSourcesDict
        = new Dictionary<GameObject, Dictionary<string, CriAtomSource>>();

    private async UniTask PlayBGMWhenReadyAsync(int version, string sheet, string cue)
    {
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
                // CriAtomSourceはStatus.Stopのときだけloopをプレーヤへ反映するため、
                // Stop直後でまだ遷移し切っていない場合に備えて明示的にも呼んでおく。
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
    }

    /// <summary> 新たに通常SE用のSourceを作る処理 </summary>
    private CriAtomSource CreateNewSESource(GameObject seObj, CueSheetType sheetType)
    {
        var newSource = seObj.AddComponent<CriAtomSource>();
        newSource.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];

        _seSourcesDict[seObj].Add(newSource);

        return newSource;
    }

    /// <summary> 再生に使うSourceを取得する。空きがあれば使い回し、なければ新規作成する </summary>
    private CriAtomSource GetOrCreateSESource(GameObject seObj, CueSheetType sheetType)
    {
        if (!_seSourcesDict.ContainsKey(seObj))
            _seSourcesDict.Add(seObj, new List<CriAtomSource>());

        foreach (var source in _seSourcesDict[seObj])
        {
            if (source.status != CriAtomSource.Status.Playing)
            {
                if (sheetType != CueSheetType.None)
                    source.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];

                return source;
            }
        }

        return CreateNewSESource(seObj, sheetType);
    }

    /// <summary> セリフキューの長さを秒で取得する。取得できない場合はフォールバック値を返す </summary>
    private float GetCueLengthSeconds(string cueName)
    {
        var acb = CriAtom.GetAcb(_cueSheetPathHolder.CueSheetPathDict[CueSheetType.PlayerVoice]);
        if (acb != null && acb.GetCueInfo(cueName, out var cueInfo) && cueInfo.length > 0)
            return cueInfo.length / 1000f;

        return VoiceDuckFallbackMaxSeconds;
    }

    /// <summary> セリフの再生終了（自然終了・StopSEどちらも含む）を監視し、ダッキングを解除する </summary>
    private async UniTask WatchVoiceDuckEndAsync(CriAtomSource source, float cueLengthSeconds)
    {
        // ボイスプール空き待ち等でstatusが進まなくなっても固定化しないよう、
        // キュー長を基準にした上限時間で必ず打ち切る。
        float startTime = Time.unscaledTime;
        float deadline = startTime + cueLengthSeconds + VoiceDuckDeadlineMarginSeconds;

        while (source != null && Time.unscaledTime < deadline)
        {
            // Play()直後はstatusがまだPlaying/Prepに遷移していないことがあるため、
            // グレース期間中はstatusによる早期終了と判定しない。
            bool pastGracePeriod = Time.unscaledTime - startTime > VoiceDuckStatusGraceSeconds;
            if (pastGracePeriod &&
                source.status != CriAtomSource.Status.Playing &&
                source.status != CriAtomSource.Status.Prep)
            {
                break;
            }
            await UniTask.Yield();
        }

        _activeVoiceDuckCount = Mathf.Max(0, _activeVoiceDuckCount - 1);
        ApplyDuckableVolumes();
    }

    /// <summary> BGM・SEの音量へ、現在のダッキング状態を反映して再適用する </summary>
    private void ApplyDuckableVolumes()
    {
        bool isDucking = _activeVoiceDuckCount > 0;

        // BGM用Sourceは1つしかないため、カテゴリを介さずSource自身のvolumeを直接操作する。
        if (_bgmSource != null)
            _bgmSource.volume = Mathf.Clamp01(_baseBGMVolume * (isDucking ? _bgmDuckRatio : 1f));

        ApplyCategoryVolume(SECategoryName, _baseSEVolume * (isDucking ? _seDuckRatio : 1f));
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
