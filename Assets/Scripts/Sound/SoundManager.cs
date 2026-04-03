using UnityEngine;
using CriWare;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance => _instance;

    /// <summary> BGM再生 </summary>
    /// <param name="cueName">再生するBGMのキュー名</param>
    /// <param name="sheetType">再生するBGMのシートの種類</param>
    public void PlayBGM(string cueName, CueSheetType sheetType = CueSheetType.None)
    {
        if (sheetType != CueSheetType.None)
            SetBGMCueSheet(_cueSheetPathHolder.CueSheetPathDict[sheetType]);

        _bgmSource.Stop();

        if (_bgmSource.cueSheet != _currentBGMCueSheet)
            _bgmSource.cueSheet = _currentBGMCueSheet;

        _bgmSource.cueName = cueName;
        _bgmSource.Play();
    }

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
        CriAtomSource newSource = CreateNewSESource(seObj);
        newSource.cueSheet = _cueSheetPathHolder.CueSheetPathDict[sheetType];
        newSource.cueName = cueName;
        newSource.Play();
    }

    private string _currentBGMCueSheet = "InGame_BGM";

    private CueSheetPathHolder _cueSheetPathHolder = new CueSheetPathHolder();

    private CriAtomSource _bgmSource;
    private Dictionary<GameObject, List<CriAtomSource>> _seSourcesDict = new Dictionary<GameObject, List<CriAtomSource>>();

    private void Awake()
    {
        Initialize();
    }

    /// <summary> 初期化 </summary>
    private void Initialize()
    {
        // BGM用ソースの設定
        _bgmSource = gameObject.AddComponent<CriAtomSource>();
        _bgmSource.cueSheet = _currentBGMCueSheet;

    }

    /// <summary> 新たにse用のSourceを作る処理 </summary>
    private CriAtomSource CreateNewSESource(GameObject seObj)
    {
        var newSource = seObj.AddComponent<CriAtomSource>();
        newSource.cueSheet = _currentBGMCueSheet;

        _seSourcesDict[seObj].Add(newSource);

        return newSource;
    }

    /// <summary> 現在のBGMのCueSheetを変更 </summary>
    private void SetBGMCueSheet(string sheetName) => _currentBGMCueSheet = sheetName;
}

