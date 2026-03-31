using UnityEngine;
using CriWare;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance => _instance;

    [SerializeField] private string _defaultCueSheet = "MainCueSheet";

    private CriAtomSource _bgmSource;
    private List<CriAtomSource> _seSources = new List<CriAtomSource>();

    void Awake()
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
        _bgmSource.cueSheet = _defaultCueSheet;
    }

    /// <summary> BGM再生 </summary>
    public void PlayBGM(string cueName)
    {
        _bgmSource.Stop();
        _bgmSource.cueName = cueName;
        _bgmSource.Play();
    }

    /// <summary> SE再生 </summary>
    public void PlaySE(string cueName)
    {
        // 停止中のソースを探して再生
        foreach (var source in _seSources)
        {
            if (source.status != CriAtomSource.Status.Playing)
            {
                source.cueName = cueName;
                source.Play();
                return;
            }
        }

        // 全てのソースが再生中の場合、新しいソースを作成して再生
        CreateNewSESource().Play();
    }

    /// <summary> 新たにse用のSourceを作る処理 </summary>
    public CriAtomSource CreateNewSESource()
    {
        var newSource = gameObject.AddComponent<CriAtomSource>();
        newSource.cueSheet = _defaultCueSheet;
        _seSources.Add(newSource);
        return newSource;
    }
}
