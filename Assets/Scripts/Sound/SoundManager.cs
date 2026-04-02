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
            SetBGMCueSheet(_cueSheetPathRegistry.CueSheetPathDict[sheetType]);

        _bgmSource.Stop();

        if (_bgmSource.cueSheet != _currentBGMCueSheet)
            _bgmSource.cueSheet = _currentBGMCueSheet;

        _bgmSource.cueName = cueName;
        _bgmSource.Play();
    }

    /// <summary> SE再生 </summary>
    /// <param name="cueName">再生するSEのキュー名</param>
    /// <param name="sheetType">再生するSEのシートの種類</param>
    public void PlaySE(string cueName, CueSheetType sheetType = CueSheetType.None)
    {
        if (sheetType != CueSheetType.None)
            SetSECueSheet(_cueSheetPathRegistry.CueSheetPathDict[sheetType]);

        // 停止中のソースを探して再生
        foreach (var source in _seSources)
        {
            if (source.status != CriAtomSource.Status.Playing)
            {
                if (source.cueSheet != _currentSECueSheet)
                    source.cueSheet = _currentSECueSheet;

                source.cueName = cueName;
                source.Play();
                return;
            }
        }

        // 全てのソースが再生中の場合、新しいソースを作成して再生
        CreateNewSESource().Play();
    }

    private static SoundManager _instance;

    private string _currentBGMCueSheet = "InGame_BGM";
    private string _currentSECueSheet = "Common_SE";

    private CueSheetPathRegistry _cueSheetPathRegistry = new CueSheetPathRegistry();

    private CriAtomSource _bgmSource;
    private List<CriAtomSource> _seSources = new List<CriAtomSource>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary> 初期化 </summary>
    private void Initialize()
    {
        // BGM用ソースの設定
        _bgmSource = gameObject.AddComponent<CriAtomSource>();
        _bgmSource.cueSheet = _currentBGMCueSheet;

        CreateNewSESource();
    }

    /// <summary> 新たにse用のSourceを作る処理 </summary>
    private CriAtomSource CreateNewSESource()
    {
        var newSource = gameObject.AddComponent<CriAtomSource>();
        newSource.cueSheet = _currentBGMCueSheet;
        _seSources.Add(newSource);
        return newSource;
    }

    /// <summary> 現在のBGMのCueSheetを変更 </summary>
    private void SetBGMCueSheet(string sheetName) => _currentBGMCueSheet = sheetName;

    /// <summary> 現在のSEのCueSheetを変更 </summary>
    private void SetSECueSheet(string sheetName) => _currentSECueSheet = sheetName;
}

