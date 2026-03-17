using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary> ゲーム内のサウンド管理クラス </summary>
public class AudioManager
{
    private static bool _isUseMainSource = false;
    /// <summary> Audio再生するClipを統括する親オブジェクト </summary>
    private static GameObject _audioObject = default;
    /// <summary> BGM用のSource（基本的に1シーンあたり1BGMなので単一のインスタンス） </summary>
    private static AudioSource _mainBGMSource = default;
    /// <summary> BGM用のSource（BGMのクロスフェードを行う際、1つだとできないため） </summary>
    private static AudioSource _subBGMSource = default;
    /// <summary> SE用のSource（SEはたくさん流れるのでList） </summary>
    private static List<AudioSource> _seSources = default;

    private static AudioHolder _soundHolder = default;

    private static AudioManager _instance = default;

    private readonly Queue<AudioClip> _seQueue = new();

    /// <summary> このクラス内で参照するBGMAudioSource </summary>
    protected AudioSource Source => _isUseMainSource ? _mainBGMSource : _subBGMSource;

    //外部のクラスで参照するBGMAudioSource
    public AudioSource BGMSource => _isUseMainSource ? _mainBGMSource : _subBGMSource;
    public List<AudioSource> SeSources => _seSources;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null) { Init(); }

            return _instance;
        }
    }

    /// <summary> AudioManagerの初期化処理 </summary>
    private static void Init()
    {
        _audioObject = new GameObject("AudioManager");
        _instance = new();

        var bgm = new GameObject("BGM");
        _mainBGMSource = bgm.AddComponent<AudioSource>();
        bgm.transform.parent = _audioObject.transform;
        _isUseMainSource = true;

        var se = new GameObject("SE");
        _seSources = new() { se.AddComponent<AudioSource>() };
        se.transform.parent = _audioObject.transform;

        _soundHolder = Resources.Load<AudioHolder>("AudioHolder");

        //初期音量設定（データ引き継ぎ等に対応する必要有）
        _mainBGMSource.volume = 1f;
        _seSources[0].volume = 1f;

        Object.DontDestroyOnLoad(_audioObject);
    }

    /// <summary> BGM再生 </summary>
    /// <param name="bgm"> どのBGMか </param>
    /// <param name="isLoop"> ループ再生するか（基本的にループする想定のためtrue） </param>
    public void PlayBGM(BGMType bgm, bool isLoop = true)
    {
        var index = -1;
        foreach (var clip in _soundHolder.BGMClips)
        {
            index++;
            if (clip.BGMType == bgm) { break; }
        }
        if (index >= _soundHolder.BGMClips.Length) { Debug.LogError("指定したBGMが見つかりませんでした"); return; }

        Source.Stop();

        Source.loop = isLoop;
        Source.clip = _soundHolder.BGMClips[index].Clip;
        Source.Play();
    }

    /// <summary> SE再生 </summary>
    /// <param name="se"> どのSEか </param>
    public void PlaySE(SEType se)
    {
        var index = -1;
        foreach (var clip in _soundHolder.SEClips)
        {
            index++;
            if (clip.SEType == se) { break; }
        }
        if (index >= _soundHolder.SEClips.Length) { Debug.LogError("指定したSEが見つかりませんでした"); return; }
        //再生するSEを追加
        _seQueue.Enqueue(_soundHolder.SEClips[index].Clip);

        //再生するSEがあれば、最後に追加したSEを再生
        if (_seQueue.Count > 0)
        {
            for (int i = 0; i < _seSources.Count; i++)
            {
                if (!_seSources[i].isPlaying) { _seSources[i].PlayOneShot(_seQueue.Dequeue()); return; }
            }

            var newSource = new GameObject("SE");
            _seSources.Add(newSource.AddComponent<AudioSource>());
            newSource.transform.parent = _audioObject.transform;

            _seSources[^1].PlayOneShot(_seQueue.Dequeue());
        }
    }

    /// <summary> BGMの再生を止める </summary>
    public void StopBGM() => Source.Stop();

    /// <summary> SEの再生を止める </summary>
    public void StopSE()
    {
        foreach (var source in _seSources) { source.Stop(); }
        _seQueue.Clear();
    }

    /// <summary> 指定したシーンのBGMを取得する </summary>
    public AudioClip GetBGMClip(BGMType bgm)
    {
        var index = -1;
        foreach (var clip in _soundHolder.BGMClips)
        {
            index++;
            if (clip.BGMType == bgm) { break; }
        }

        return _soundHolder.BGMClips[index].Clip;
    }

    /// <summary> BGMの再生終了待機 </summary>
    public IEnumerator BGMPlayingWait()
    {
        yield return new WaitUntil(() => !Source.isPlaying);
    }

    public async UniTask BGMPlaying()
    {
        while (Source.isPlaying) { await Task.Yield(); }
    }

    /// <summary> SEの再生終了待機 </summary>
    public IEnumerator SEPlayingWait()
    {
        foreach (var source in _seSources)
        {
            yield return new WaitUntil(() => !source.isPlaying);
        }
    }

    public async UniTask SEPlaying()
    {
        foreach (var source in _seSources)
        {
            while (source.isPlaying) { await Task.Yield(); }
        }
    }

    /// <summary> BGM用のAudioSourceを変更する </summary>
    /// <param name="clip"> 新しく再生する音源（指定があれば設定する） </param>
    public void ChangeBGMSource(AudioClip clip = null)
    {
        _isUseMainSource = !_isUseMainSource;

        //2つ目のBGMSourceを初めて使うとき、インスタンスを生成する
        if (!_isUseMainSource && _subBGMSource == null)
        {
            var bgm = new GameObject("SubBGM");
            _subBGMSource = bgm.AddComponent<AudioSource>();
            bgm.transform.parent = _audioObject.transform;
        }
        if (clip != null) { Source.clip = clip; }
    }

    #region 以下Audio系パラメーター設定用の関数
    /// <summary> BGMの音量設定 </summary>
    public void VolumeSettingBGM(float value)
    {
        if (Source == null) { return; }

        Source.volume = value;
    }

    /// <summary> SEの音量設定 </summary>
    public void VolumeSettingSE(float value)
    {
        if (_seSources == null || _seSources.Count <= 0) { return; }

        foreach (var source in _seSources) { source.volume = value; }
    }
    #endregion
}

#region Audio Extension
/// <summary> Audio再生に関する拡張メソッドをまとめたクラス </summary>
public static class AudioExtensions
{
    private static bool _isFadePlaying = false;

    /// <summary> 音源のクロスフェード </summary>
    /// <param name="source"> 現在のAudioSource </param>
    /// <param name="next"> 次に再生する音源 </param>
    /// <param name="duration"> 実行時間 </param>
    public static async void CrossFade(this AudioSource source, AudioClip next, float duration)
    {
        await CrossFadeAsync(source, next, duration);
    }

    private static async Task CrossFadeAsync(AudioSource source, AudioClip next, float duration)
    {
        var targetVolume = source.volume;
        var endVolume = 0f;

        var audioFadeOut = Task.Run(() => AudioFadeAsync(source, endVolume, duration));
        AudioManager.Instance.ChangeBGMSource(next);
        var audioFadeIn = Task.Run(() => AudioFadeAsync(AudioManager.Instance.BGMSource, targetVolume, duration));

        //複数のTaskを並列実行し、全てが終了するまで待機する
        await Task.WhenAll(audioFadeOut, audioFadeIn);
    }

    /// <summary> 音量を徐々に変更する </summary>
    /// <param name="source"> 対象のAudioSource </param>
    /// <param name="endVolume"> 最終的な音量 </param>
    /// <param name="duration"> 実行時間 </param>
    public static async void DOFade(this AudioSource source, float endVolume, float duration)
    {
        await AudioFadeAsync(source, endVolume, duration);
    }

    private static async Task AudioFadeAsync(AudioSource source, float endVolume, float duration)
    {
        if (_isFadePlaying) { Debug.Log("AudioFade Playing..."); return; }

        _isFadePlaying = true;
        var currentVolume = source.volume;
        if ((int)currentVolume == (int)endVolume) { return; }

        float timer = 0f;
        while (timer <= duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(currentVolume, endVolume, timer / duration);

            await Task.Yield();
        }

        source.volume = endVolume;
        _isFadePlaying = false;
    }
}
#endregion
