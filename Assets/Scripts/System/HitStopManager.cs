using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

/// <summary>
/// ヒットストップを管理するマネージャー。
/// グループ別に ISpeedChange を登録し、
/// HitStopData と命中結果を受け取ってヒットストップを発動する。
/// </summary>
public sealed class HitStopManager : IDisposable
{
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
        if (target == null) { return; }

        if (_groupTargets.TryGetValue(group, out var list) &&
            !list.Contains(target))
        {
            list.Add(target);

            ApplyTargetSpeedScale(target);
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
        IReadOnlyList<ISpeedChange> hitEnemyTargets = null)
    {
        if (data == null)
        {
            return;
        }

        float duration =
            data.GetDuration(isWeakPoint, isArmorBreak, isKill);

        ExecuteHitStopAsync(
            duration,
            data.TimeScale,
            data.TargetGroup,
            data.Priority,
            hitEnemyTargets
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
       int priority = int.MaxValue,
       IReadOnlyList<ISpeedChange> hitEnemyTargets = null)
    {
        ExecuteHitStopAsync(
            duration,
            timeScale,
            targetGroup,
            priority,
            hitEnemyTargets
        ).Forget();
    }

    public IDisposable BeginManualStop(
       HitStopTargetGroup targetGroup,
       float timeScale = 0f,
       int priority = int.MinValue,
       IReadOnlyList<ISpeedChange> hitEnemyTargets = null)
    {
        var stop = new ManualStop(targetGroup, timeScale, priority, hitEnemyTargets);
        _manualStops.Add(stop);
        ApplyResolvedSpeedScale(targetGroup);

        return new ManualStopHandle(this, stop);
    }

    /// <summary>
    /// 現在発動中のヒットストップを即時キャンセルし、速度を元に戻す
    /// </summary>
    public void Cancel()
    {
        var stops = _timedStops.ToArray();
        _timedStops.Clear();
        _manualStops.Clear();
        ApplyResolvedSpeedScale(HitStopTargetGroup.All);

        foreach (var stop in stops)
        {
            stop.Cancellation.Cancel();
        }
    }

    /// <summary>全対象の速度を戻してマネージャーを破棄する。</summary>
    public void Dispose()
    {
        Cancel();

        foreach (var list in _groupTargets.Values)
        {
            list.Clear();
        }

        ServiceLocator.Unregister<HitStopManager>();
    }

    private readonly Dictionary<HitStopTargetGroup, List<ISpeedChange>> _groupTargets =
        new()
        {
            { HitStopTargetGroup.Player, new List<ISpeedChange>() },
            { HitStopTargetGroup.HitEnemy, new List<ISpeedChange>() },
            { HitStopTargetGroup.AllEnemies, new List<ISpeedChange>() },
            { HitStopTargetGroup.Effects, new List<ISpeedChange>() },
            { HitStopTargetGroup.Camera, new List<ISpeedChange>() },
            { HitStopTargetGroup.ThunderGauge, new List<ISpeedChange>() },
            { HitStopTargetGroup.Time, new List<ISpeedChange>() },
        };

    private readonly List<TimedStop> _timedStops = new();
    private readonly List<ManualStop> _manualStops = new();

    private async UniTaskVoid ExecuteHitStopAsync(
        float duration,
        float timeScale,
        HitStopTargetGroup targetGroups,
        int priority,
        IReadOnlyList<ISpeedChange> hitEnemyTargets)
    {
        // 呼び出し側のリストが変更されても、今回の命中対象を維持する。
        var hitTargets = hitEnemyTargets == null ? null : new HashSet<ISpeedChange>(hitEnemyTargets);
        var stop = new TimedStop(
            timeScale,
            priority,
            target => GetTargetMatch(target, targetGroups, hitTargets));

        foreach (var previousStop in _timedStops)
        {
            // グループ名ではなく対象の重なりを除く。AllEnemies と HitEnemy の重複にも対応し、
            // 未生成のエフェクトにも同じ条件を適用する。上書き前のスローは後から復活させない。
            var previousTargets = previousStop.ContainsTarget;
            var nextTargets = stop.ContainsTarget;
            if (priority <= previousStop.Priority)
            {
                previousStop.ContainsTarget = target => previousTargets(target) && !nextTargets(target);
            }
            else
            {
                // 優先度で無視するのは重複対象だけ。別の対象には今回の演出を適用する。
                stop.ContainsTarget = target => nextTargets(target) && !previousTargets(target);
            }
        }

        _timedStops.Add(stop);
        try
        {
            ApplyResolvedSpeedScale(targetGroups);
            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                stop.Cancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_timedStops.Remove(stop))
            {
                ApplyResolvedSpeedScale(targetGroups);
            }

            stop.Cancellation.Dispose();
        }
    }

    private bool GetTargetMatch(
        ISpeedChange target,
        HitStopTargetGroup targetGroups,
        ICollection<ISpeedChange> hitEnemyTargets)
    {
        foreach (var (group, targets) in _groupTargets)
        {
            if ((targetGroups & group) == 0 || !targets.Contains(target))
            {
                continue;
            }

            if (group != HitStopTargetGroup.HitEnemy ||
                hitEnemyTargets == null ||
                hitEnemyTargets.Contains(target))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyResolvedSpeedScale(HitStopTargetGroup targetGroups)
    {
        // 同じ敵が複数グループに登録されるため、対象ごとに一度だけ最終速度を決める。
        var targets = new HashSet<ISpeedChange>();
        foreach (var (group, registeredTargets) in _groupTargets)
        {
            if ((targetGroups & group) != 0)
            {
                targets.UnionWith(registeredTargets);
            }
        }

        foreach (var target in targets)
        {
            ApplyTargetSpeedScale(target);
        }
    }

    private void ApplyTargetSpeedScale(ISpeedChange target)
    {
        ManualStop activeManualStop = null;
        foreach (var stop in _manualStops)
        {
            if (GetTargetMatch(target, stop.TargetGroup, stop.HitEnemyTargets) &&
                (activeManualStop == null || stop.Priority < activeManualStop.Priority))
            {
                activeManualStop = stop;
            }
        }

        if (activeManualStop != null)
        {
            target.OnSpeedChange(activeManualStop.TimeScale);
            return;
        }

        foreach (var stop in _timedStops)
        {
            if (stop.ContainsTarget(target))
            {
                target.OnSpeedChange(stop.TimeScale);
                return;
            }
        }

        target.OnSpeedChange(1f);
    }

    private void EndManualStop(ManualStop stop)
    {
        if (stop != null && _manualStops.Remove(stop))
        {
            ApplyResolvedSpeedScale(stop.TargetGroup);
        }
    }

    private sealed class TimedStop
    {
        public float TimeScale { get; }
        public int Priority { get; }
        public Func<ISpeedChange, bool> ContainsTarget { get; set; }
        public CancellationTokenSource Cancellation { get; } = new();

        public TimedStop(float timeScale, int priority, Func<ISpeedChange, bool> containsTarget)
        {
            TimeScale = timeScale;
            Priority = priority;
            ContainsTarget = containsTarget;
        }
    }

    private sealed class ManualStop
    {
        public HitStopTargetGroup TargetGroup { get; }
        public float TimeScale { get; }
        public int Priority { get; }
        public HashSet<ISpeedChange> HitEnemyTargets { get; }

        public ManualStop(
            HitStopTargetGroup targetGroup,
            float timeScale,
            int priority,
            IReadOnlyList<ISpeedChange> hitEnemyTargets)
        {
            TargetGroup = targetGroup;
            TimeScale = timeScale;
            Priority = priority;
            HitEnemyTargets = hitEnemyTargets == null ? null : new HashSet<ISpeedChange>(hitEnemyTargets);
        }
    }

    private sealed class ManualStopHandle : IDisposable
    {
        public ManualStopHandle(HitStopManager owner, ManualStop stop)
        {
            _owner = owner;
            _stop = stop;
        }

        public void Dispose()
        {
            if (_owner == null)
            {
                return;
            }

            _owner.EndManualStop(_stop);
            _owner = null;
            _stop = null;
        }

        private HitStopManager _owner;
        private ManualStop _stop;
    }
}