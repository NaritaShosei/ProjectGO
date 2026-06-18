using System;
using UnityEngine;

/// <summary>
/// フェーズタイマーのプレゼンタークラス。UIの更新やイベントの管理を担当する。
/// </summary>
public class CountDownTimerPresenter : IDisposable
{
    /// <summary>
    /// コンストラクタ。必要な初期化を行う。
    /// </summary>
    public CountDownTimerPresenter(CountDownTimer timer,IPhaseTimerView view)
    {
        _timer = timer;
        _view = view;   

        // タイマーのイベントにリスナーを登録
        timer.OnTimerStarted += HandleTimerStarted;
        timer.OnTimeChanged += HandleTimeChanged;
    }

    public void Dispose()
    {
        // タイマーのイベントからリスナーを解除
        _timer.OnTimerStarted -= HandleTimerStarted;
        _timer.OnTimeChanged -= HandleTimeChanged;
    }

    private readonly CountDownTimer _timer;
    private readonly IPhaseTimerView _view;

    /// <summary> タイマー開始イベントのハンドラー。UIを初期化する。</summary>
    private void HandleTimerStarted(float maxTime)
    {
        _view.UpdateTimer(maxTime, maxTime);
    }

    /// <summary> タイマー更新イベントのハンドラー。UIを更新する。</summary>
    private void HandleTimeChanged(float currentTime, float maxTime)
    {
        _view.UpdateTimer(currentTime, maxTime);
    }
}
