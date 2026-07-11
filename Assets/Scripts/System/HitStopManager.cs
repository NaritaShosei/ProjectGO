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

            // ヒットストップ中なら即適用
            if (_currentScale.TryGetValue(group, out var scale) && Mathf.Abs(scale - 1f) > 0.0001f)
            {
                if (group != HitStopTargetGroup.HitEnemy ||
                  _activeHitEnemyTargets == null ||
                  _activeHitEnemyTargets.Contains(target))
                {
                    target.OnSpeedChange(scale);
                }
            }
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
        _hitStopCancellation?.Cancel();
        _manualStops.Clear();
        ApplyTimedSpeedScale(1f, ~HitStopTargetGroup.None, null);
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

        _manualStops.Clear();

        ServiceLocator.Unregister<HitStopManager>();
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
            { HitStopTargetGroup.ThunderGauge, new List<ISpeedChange>() },
        };

    /// <summary>
    /// グループごとの現在の速度倍率
    /// </summary>
    private readonly Dictionary<HitStopTargetGroup, float> _currentScale =
        new()
        {
        { HitStopTargetGroup.Player,     1f },
        { HitStopTargetGroup.HitEnemy,   1f },
        { HitStopTargetGroup.AllEnemies, 1f },
        { HitStopTargetGroup.Effects,    1f },
        { HitStopTargetGroup.Camera,     1f },
        { HitStopTargetGroup.ThunderGauge, 1f },
        };

    private readonly Dictionary<HitStopTargetGroup, float> _timedScale =
        new()
        {
        { HitStopTargetGroup.Player,     1f },
        { HitStopTargetGroup.HitEnemy,   1f },
        { HitStopTargetGroup.AllEnemies, 1f },
        { HitStopTargetGroup.Effects,    1f },
        { HitStopTargetGroup.Camera,     1f },
        { HitStopTargetGroup.ThunderGauge, 1f },
        };

    private readonly List<ManualStop> _manualStops = new();

    /// <summary>
    /// 現在発動中のヒットストップ用キャンセルトークン
    /// </summary>
    private CancellationTokenSource _hitStopCancellation;

    private HashSet<ISpeedChange> _activeHitEnemyTargets;
    private IReadOnlyList<ISpeedChange> _timedHitEnemyTargets;

    private int _currentPriority = int.MaxValue;

    /// <summary>
    /// ヒットストップの非同期処理本体
    /// </summary>
    private async UniTaskVoid ExecuteHitStopAsync(
    float duration,
    float timeScale,
    HitStopTargetGroup targetGroups,
    int priority,
    IReadOnlyList<ISpeedChange> hitEnemyTargets)
    {
        // 既により高優先度が動いているなら無視
        if (_hitStopCancellation != null &&
            priority > _currentPriority)
        {
            return;
        }

        _currentPriority = priority;

        _hitStopCancellation?.Cancel();
        _hitStopCancellation?.Dispose();

        var cancellation = new CancellationTokenSource();
        _hitStopCancellation = cancellation;

        ApplyTimedSpeedScale(timeScale, targetGroups, hitEnemyTargets);

        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                cancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_hitStopCancellation, cancellation))
            {
                ApplyTimedSpeedScale(1f, targetGroups, hitEnemyTargets);

                _currentPriority = int.MaxValue;

                _hitStopCancellation.Dispose();
                _hitStopCancellation = null;
            }
        }
    }

    /// <summary>
    /// 指定グループの ISpeedChange に速度変更を適用する
    /// </summary>
    private void ApplyTimedSpeedScale(
    float scale,
    HitStopTargetGroup targetGroups,
    IReadOnlyList<ISpeedChange> hitEnemyTargets)
    {
        foreach (var group in _groupTargets.Keys.ToArray())
        {
            if ((targetGroups & group) == 0) continue;

            _timedScale[group] = scale;
        }

        if ((targetGroups & HitStopTargetGroup.HitEnemy) != 0)
        {
            _timedHitEnemyTargets = Mathf.Abs(scale - 1f) > 0.0001f
                ? hitEnemyTargets
                : null;
        }

        ApplyResolvedSpeedScale(targetGroups);
    }

    private void ApplyResolvedSpeedScale(HitStopTargetGroup targetGroups)
    {
        foreach (var (group, list) in _groupTargets)
        {
            if ((targetGroups & group) == 0) continue;

            var manualStop = GetActiveManualStop(group);
            float scale = manualStop?.TimeScale ?? _timedScale[group];

            _currentScale[group] = scale;

            if (group == HitStopTargetGroup.HitEnemy)
            {
                _activeHitEnemyTargets =
                (Mathf.Abs(scale - 1f) > 0.0001f && manualStop?.HitEnemyTargets != null)
                    ? new HashSet<ISpeedChange>(manualStop.HitEnemyTargets)
                    : (Mathf.Abs(scale - 1f) > 0.0001f && _timedHitEnemyTargets != null)
                    ? new HashSet<ISpeedChange>(_timedHitEnemyTargets)
                    : null;
            }

            foreach (var target in list.ToArray())
            {
                if (group == HitStopTargetGroup.HitEnemy &&
                    _activeHitEnemyTargets != null &&
                    !_activeHitEnemyTargets.Contains(target))
                {
                    continue;
                }

                target.OnSpeedChange(scale);
            }
        }
    }

    private ManualStop GetActiveManualStop(HitStopTargetGroup group)
    {
        ManualStop activeStop = null;

        foreach (var stop in _manualStops)
        {
            if ((stop.TargetGroup & group) == 0)
            {
                continue;
            }

            if (activeStop == null || stop.Priority < activeStop.Priority)
            {
                activeStop = stop;
            }
        }

        return activeStop;
    }

    private void EndManualStop(ManualStop stop)
    {
        if (stop == null || !_manualStops.Remove(stop))
        {
            return;
        }

        ApplyResolvedSpeedScale(stop.TargetGroup);
    }

    private sealed class ManualStop
    {
        public ManualStop(
            HitStopTargetGroup targetGroup,
            float timeScale,
            int priority,
            IReadOnlyList<ISpeedChange> hitEnemyTargets)
        {
            TargetGroup = targetGroup;
            TimeScale = timeScale;
            Priority = priority;
            HitEnemyTargets = hitEnemyTargets;
        }

        public HitStopTargetGroup TargetGroup { get; }
        public float TimeScale { get; }
        public int Priority { get; }
        public IReadOnlyList<ISpeedChange> HitEnemyTargets { get; }
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

    private void ApplySpeedScale(
    float scale,
    HitStopTargetGroup targetGroups,
    IReadOnlyList<ISpeedChange> hitEnemyTargets)
    {
        foreach (var (group, list) in _groupTargets)
        {
            if ((targetGroups & group) == 0) continue;

            // スケールを記録
            _currentScale[group] = scale;

            if (group == HitStopTargetGroup.HitEnemy)
            {
                _activeHitEnemyTargets =
                (Mathf.Abs(scale - 1f) > 0.0001f && hitEnemyTargets != null)
                    ? new HashSet<ISpeedChange>(hitEnemyTargets)
                    : null;
            }

            foreach (var target in list.ToArray())
            {
                if (group == HitStopTargetGroup.HitEnemy &&
                    hitEnemyTargets != null &&
                    !hitEnemyTargets.Contains(target))
                {
                    continue;
                }

                target.OnSpeedChange(scale);
            }
        }
    }
}
