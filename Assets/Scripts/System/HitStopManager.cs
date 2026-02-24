// HitStopManager.cs
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ヒットストップを管理するマネージャー
/// ISpeedChangeを実装したオブジェクトを登録・解除して対象を増減できる
/// </summary>
public class HitStopManager : MonoBehaviour
{
    /// <summary>
    /// ヒットストップ対象を登録
    /// </summary>
    public void Register(ISpeedChange target)
    {
        if (target == null || _targets.Contains(target)) return;
        _targets.Add(target);
    }

    /// <summary>
    /// ヒットストップ対象を解除
    /// </summary>
    public void Unregister(ISpeedChange target)
    {
        if (target == null || !_targets.Contains(target)) return;
        _targets.Remove(target);
    }

    /// <summary>
    /// ヒットストップを発動
    /// </summary>
    /// <param name="duration">停止時間（秒）</param>
    /// <param name="timeScale">停止中のスケール（0で完全停止）</param>
    public void TriggerHitStop(float duration, float timeScale = 0f)
    {
        HitStopAsync(duration, timeScale).Forget();
    }

    private readonly List<ISpeedChange> _targets = new();
    private float _currentScale = 1f;
    private CancellationTokenSource _hitStopCts;

    private async UniTaskVoid HitStopAsync(float duration, float timeScale)
    {

        _hitStopCts?.Cancel();
        _hitStopCts?.Dispose();
        _hitStopCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

        ApplyScale(timeScale);

        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                delayType: DelayType.UnscaledDeltaTime,
                cancellationToken: destroyCancellationToken
            );

            ApplyScale(1f);
        }
        catch(OperationCanceledException)
        {
            // ヒットストップがキャンセルされた場合は何もしない
        }
        finally
        {
            _hitStopCts.Dispose();
            _hitStopCts = null;
        }
    }

    private void ApplyScale(float scale)
    {
        _currentScale = scale;

        foreach (var target in _targets)
        {
            target.OnSpeedChange(_currentScale);
        }
    }

    private void OnDestroy()
    {
        _hitStopCts?.Cancel();
        _hitStopCts?.Dispose();
        _targets.Clear();
    }
}
