using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class CountDownTimer : IDisposable
{
    public float CurrentTime => _currentTime;
    public float MaxTime => _maxTime;

    /// <summary> 時間切れイベント</summary>
    public event Action OnTimeEnded;

    /// <summary> タイマーを開始</summary>
    public void StartTimer(float maxTime)
    {
        StopTimer();

        _maxTime = maxTime;
        _currentTime = maxTime;

        _timerCts = new CancellationTokenSource();

        UpdateTimeAsync().Forget();
    }

    /// <summary> タイマーを停止</summary>
    public void StopTimer()
    {
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _timerCts = null;
    }

    /// <summary> タイマーを一時停止</summary>
    public void PauseTimer()
    {
        _pauseCount++;
    }

    /// <summary> タイマーを再開</summary>
    public void ResumeTimer()
    {
        _pauseCount = Mathf.Max(0, _pauseCount - 1);
    }

    public void Dispose()
    {
        StopTimer();
    }

    private float _maxTime;
    private float _currentTime;

    private int _pauseCount;

    private CancellationTokenSource _timerCts;

    /// <summary> タイマーの時間を更新する非同期メソッド</summary>
    private async UniTask UpdateTimeAsync()
    {
        var token = _timerCts.Token;

        try
        {
            while (_currentTime > 0)
            {
                await UniTask.Yield(token);

                if (_pauseCount == 0)
                    _currentTime -= Time.deltaTime;
            }

            _currentTime = 0;
            StopTimer();
            OnTimeEnded?.Invoke();
        }
        catch (OperationCanceledException)
        {
        }
    }
}
