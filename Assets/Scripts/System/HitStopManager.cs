using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// ヒットストップを管理するマネージャー。
/// グループ別に ISpeedChange を登録し、
/// HitStopData と命中結果を受け取ってヒットストップを発動する。
/// </summary>
public sealed class HitStopManager : IDisposable
{
    // =============================
    // Constructor
    // =============================

    /// <summary>
    /// ヒットストップマネージャーを生成し、ServiceLocator に登録する
    /// </summary>
    public HitStopManager()
    {
        ServiceLocator.Register(this);
    }

    /// <summary>
    /// 指定グループにヒットストップ対象を登録する
    /// </summary>
    public void Register(ISpeedChange target, HitStopTargetGroup group)
    {
        if (target == null) return;

        if (_groupTargets.TryGetValue(group, out var list) &&
            !list.Contains(target))
        {
            list.Add(target);
        }
    }

    /// <summary>
    /// 指定グループからヒットストップ対象を解除する
    /// </summary>
    public void Unregister(ISpeedChange target, HitStopTargetGroup group)
    {
        if (target == null) return;

        _groupTargets.GetValueOrDefault(group)?.Remove(target);
    }

    /// <summary>
    /// 全グループからヒットストップ対象を解除する
    /// （オブジェクト破棄時などに使用）
    /// </summary>
    public void UnregisterFromAll(ISpeedChange target)
    {
        if (target == null) return;

        foreach (var list in _groupTargets.Values)
        {
            list.Remove(target);
        }
    }

    /// <summary>
    /// HitStopData と命中結果を元にヒットストップを発動する
    /// </summary>
    /// <param name="data">攻撃データが持つ HitStopData</param>
    /// <param name="isWeakPoint">弱点ヒットか</param>
    /// <param name="isArmorBreak">鎧破壊が発生したか</param>
    /// <param name="isKill">撃破したか</param>
    /// <param name="hitEnemyTarget">
    /// ヒットした敵（HitEnemy グループの絞り込みに使用）
    /// </param>
    public void Trigger(
        HitStopData data,
        bool isWeakPoint = false,
        bool isArmorBreak = false,
        bool isKill = false,
        ISpeedChange hitEnemyTarget = null)
    {
        if (data == null) return;

        float duration = data.GetDuration(isWeakPoint, isArmorBreak, isKill);

        ExecuteHitStopAsync(
            duration,
            data.TimeScale,
            data.TargetGroup,
            hitEnemyTarget
        ).Forget();
    }

    /// <summary>
    /// 時間・対象を直接指定してヒットストップを発動する
    /// （必殺技・死亡演出など特殊ケース用）
    /// </summary>
    public void TriggerDirect(
        float duration,
        HitStopTargetGroup targetGroup,
        float timeScale = 0f,
        ISpeedChange hitEnemyTarget = null)
    {
        ExecuteHitStopAsync(
            duration,
            timeScale,
            targetGroup,
            hitEnemyTarget
        ).Forget();
    }

    /// <summary>
    /// 現在発動中のヒットストップを即時キャンセルし、速度を元に戻す
    /// </summary>
    public void Cancel()
    {
        _hitStopCancellation?.Cancel();
        ApplySpeedScale(1f, ~HitStopTargetGroup.None, null);
    }

    /// <summary>
    /// マネージャーを破棄し、全リソースを解放する
    /// </summary>
    public void Dispose()
    {
        _hitStopCancellation?.Cancel();
        _hitStopCancellation?.Dispose();
        _hitStopCancellation = null;

        foreach (var list in _groupTargets.Values)
        {
            list.Clear();
        }
    }

    /// <summary>
    /// グループごとのヒットストップ対象一覧
    /// </summary>
    private readonly Dictionary<HitStopTargetGroup, List<ISpeedChange>> _groupTargets =
        new()
        {
            { HitStopTargetGroup.Player,     new List<ISpeedChange>() },
            { HitStopTargetGroup.HitEnemy,   new List<ISpeedChange>() },
            { HitStopTargetGroup.AllEnemies, new List<ISpeedChange>() },
            { HitStopTargetGroup.Effects,    new List<ISpeedChange>() },
            { HitStopTargetGroup.Camera,     new List<ISpeedChange>() },
        };

    /// <summary>
    /// 現在発動中のヒットストップ用キャンセルトークン
    /// </summary>
    private CancellationTokenSource _hitStopCancellation;


    /// <summary>
    /// ヒットストップの非同期処理本体
    /// </summary>
    private async UniTaskVoid ExecuteHitStopAsync(
        float duration,
        float timeScale,
        HitStopTargetGroup targetGroups,
        ISpeedChange hitEnemyTarget)
    {
        // 既存ヒットストップをキャンセル
        _hitStopCancellation?.Cancel();
        _hitStopCancellation?.Dispose();

        var cancellation = new CancellationTokenSource();
        _hitStopCancellation = cancellation;

        ApplySpeedScale(timeScale, targetGroups, hitEnemyTarget);

        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                cancellation.Token
            );

            ApplySpeedScale(1f, targetGroups, hitEnemyTarget);
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は何もしない
        }
        finally
        {
            if (ReferenceEquals(_hitStopCancellation, cancellation))
            {
                _hitStopCancellation.Dispose();
                _hitStopCancellation = null;
            }
        }
    }

    /// <summary>
    /// 指定グループの ISpeedChange に速度変更を適用する
    /// </summary>
    private void ApplySpeedScale(
        float scale,
        HitStopTargetGroup targetGroups,
        ISpeedChange hitEnemyTarget)
    {
        foreach (var (group, list) in _groupTargets)
        {
            if ((targetGroups & group) == 0) continue;

            foreach (var target in list.ToArray())
            {
                // HitEnemy グループはヒットした敵 1 体のみに適用
                if (group == HitStopTargetGroup.HitEnemy &&
                    hitEnemyTarget != null &&
                    !ReferenceEquals(target, hitEnemyTarget))
                {
                    continue;
                }

                target.OnSpeedChange(scale);
            }
        }
    }
}
