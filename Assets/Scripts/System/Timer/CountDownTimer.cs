using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class CountDownTimer : IDisposable, ISpeedChange
{
    public float CurrentTime => _currentTime;
    public float MaxTime => _maxTime;
    public float TimeScale => _timeScale;

    /// <summary> 時間切れイベント</summary>
    public event Action OnTimeEnded;

    /// <summary>タイマー開始イベント。引数は最大時間</summary>
    public event Action<float> OnTimerStarted;

    /// <summary>毎フレームの時間更新イベント。引数は（現在時間, 最大時間）</summary>
    public event Action<float, float> OnTimeChanged;

    public CountDownTimer()
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Register(this, HitStopTargetGroup.Time);
        }
    }

    /// <summary> タイマーを開始</summary>
    public void StartTimer(float maxTime)
    {
        StopTimer();

        _maxTime = maxTime;
        _currentTime = maxTime;
        _pauseCount = 0;

        _timerCts = new CancellationTokenSource();

        OnTimerStarted?.Invoke(maxTime);

        UpdateTimeAsync().Forget();
    }

    /// <summary> タイマーを停止</summary>
    public void StopTimer()
    {
        if (_timerCts == null) return;

        _timerCts.Cancel();
        _timerCts.Dispose();
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

    public void OnSpeedChange(float scale)
    {
        _timeScale = Mathf.Max(0f, scale);
    }

    public void Dispose()
    {
        StopTimer();

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Unregister(this, HitStopTargetGroup.Time);
        }
    }

    private float _maxTime;
    private float _currentTime;

    private int _pauseCount;
    private float _timeScale = 1f;

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
                {
                    _currentTime = Mathf.Max(0f, _currentTime - Time.deltaTime * _timeScale);
                    OnTimeChanged?.Invoke(_currentTime, _maxTime);
                }
            }

            StopTimer();
            OnTimeEnded?.Invoke();
        }
        catch (OperationCanceledException)
        {
        }
    }
}
