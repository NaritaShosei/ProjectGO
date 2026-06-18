using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class MoviePlayer : MonoBehaviour
{
    // 仮置き      
    public event Action OnMovieStarted;
    public event Action OnMovieFinished;

    public bool PlayMovie(string movieName)
    {
        if (_movieDictionary.TryGetValue(movieName, out var timelineAsset))
        {
            _playableDirector.playableAsset = timelineAsset;
            _playableDirector.Play();

            OnMovieStarted?.Invoke();
            return true;
        }

        Debug.LogWarning($"指定されたムービー名 '{movieName}' は存在しません。");
        return false;
    }

    public void StopMovie()
    {
        if (_playableDirector.state == PlayState.Playing)
        {
            _playableDirector.Stop();
        }
    }

    // TODO:この方式だとよくない挙動が起きるかもしれない。要検討。
    public void SkipMovie()
    {
        if (_playableDirector.playableAsset == null)
            return;

        if (_playableDirector.state != PlayState.Playing)
            return;

        _playableDirector.time = _playableDirector.duration;
        _playableDirector.Evaluate();
        _playableDirector.Stop();
    }

    [SerializeField] private PlayableDirector _playableDirector;
    [SerializeField] private MovieData[] _movieData;

    private readonly Dictionary<string, TimelineAsset> _movieDictionary = new Dictionary<string, TimelineAsset>();

    private void Awake()
    {
        if (_playableDirector == null)
        {
            Debug.LogError("PlayableDirector がアタッチされていません。MoviePlayer を正しく動作させるために、PlayableDirector をアタッチしてください。");
            return;
        }

        BuildDictionary();
    }
    private void OnEnable()
    {
        if (_playableDirector != null)
            _playableDirector.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        if (_playableDirector != null)
            _playableDirector.stopped -= OnDirectorStopped;
    }

    private void BuildDictionary()
    {
        if (_movieData == null || _movieData.Length == 0)
        {
            Debug.LogWarning("MovieData が設定されていません。MoviePlayer の設定を確認してください。");
            return;
        }

        foreach (var data in _movieData)
        {
            if (data.TimelineAsset == null)
            {
                Debug.LogWarning("TimeLineAsset が null です。MovieData の設定を確認してください。");
                continue;
            }

            if (string.IsNullOrWhiteSpace(data.Name) || _movieDictionary.ContainsKey(data.Name))
            {
                Debug.LogWarning($"MovieData の Name が重複しているか、空文字です。Name: {data.Name}");
                continue;
            }

            _movieDictionary.Add(data.Name, data.TimelineAsset);
        }
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        OnMovieFinished?.Invoke();
    }
}

[Serializable]
public struct MovieData
{
    public TimelineAsset TimelineAsset => _timelineAsset;
    public string Name => _name;

    [Header("Movie Data")]
    [SerializeField] private TimelineAsset _timelineAsset;
    [SerializeField] private string _name;
}
