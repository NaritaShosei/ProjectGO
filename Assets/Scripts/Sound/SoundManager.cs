using UnityEngine;
using CriWare;
using System.Collections.Generic;

public class SoundManager 
{
    /// <summary> コンストラクタ </summary>
    /// <param name="bgmPlayer"> BGM用のPlayerObject </param>
    /// <param name="defaultBGM"> BGMの音源データが入ったCueSheetの名前(あとで再生時に変更可能) </param>
    public SoundManager(GameObject bgmPlayer, string defaultBGM)
    {
        _defaultBGMCueSheet = defaultBGM;
        Initialize(bgmPlayer);
    }

    /// <summary> BGM再生 </summary>
    /// <param name="cueName">再生するBGMのキュー名</param>
    /// <param name="sheetType">再生するBGMのシートの種類</param>
    public void PlayBGM(string cueName, CueSheetType sheetType = CueSheetType.None)
    {
        _bgmSource.Stop();

        if(sheetType != CueSheetType.None)
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
        CriAtomSource newSource = CreateNewSESource(seObj, sheetType);
        newSource.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];
        newSource.cueName = cueName;
        newSource.Play();
    }

    /// <summary> SE停止 </summary>
    public void StopSE(GameObject seObj)
    {
        if (_seSourcesDict.ContainsKey(seObj))
        {
            foreach (var source in _seSourcesDict[seObj])
            {
                source.Stop();
            }
        }
    }

    /// <summary> SE一時停止 </summary>
    public void PauseSE(GameObject seObj)
    {
        if (_seSourcesDict.ContainsKey(seObj))
        {
            foreach (var source in _seSourcesDict[seObj])
            {
                source.Pause(true);
            }
        }
    }

    /// <summary> SE再開 </summary>
    public void ResumeSE(GameObject seObj)
    {
        if (_seSourcesDict.ContainsKey(seObj))
        {
            foreach (var source in _seSourcesDict[seObj])
            {
                source.Pause(false);
            }
        }
    }

    // デフォルトのBGM CueSheet名
    private readonly string _defaultBGMCueSheet;

    // CueSheetのパスを管理するクラス
    private CueSheetPathHolder _cueSheetPathHolder = new CueSheetPathHolder();

    // BGM用のソース
    private CriAtomSource _bgmSource;

    // SE用のソースを管理するDictionary
    private Dictionary<GameObject, List<CriAtomSource>> _seSourcesDict = new Dictionary<GameObject, List<CriAtomSource>>();

    /// <summary> 初期化 </summary>
    private void Initialize(GameObject bgmPlayer)
    {
        // BGM用ソースの設定
        if(!bgmPlayer.TryGetComponent<CriAtomSource>(out _bgmSource))
            _bgmSource = bgmPlayer.AddComponent<CriAtomSource>();

        _bgmSource.cueSheet = _defaultBGMCueSheet;
    }

    /// <summary> 新たにse用のSourceを作る処理 </summary>
    private CriAtomSource CreateNewSESource(GameObject seObj, CueSheetType sheetType)
    {
        var newSource = seObj.AddComponent<CriAtomSource>();
        newSource.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];

        _seSourcesDict[seObj].Add(newSource);

        return newSource;
    }
}

