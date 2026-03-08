using System;
using UnityEngine;

public class EnemyGaugePresenter : IDisposable
{
    public EnemyGaugeView View { get; }

    public EnemyGaugePresenter(
        IEnemy enemy,
        EnemyGaugeView view,
        Transform playerTransform,
        float detectionRange,
        float damagedDisplayDuration)
    {
        _enemy = enemy;
        _playerTransform = playerTransform;
        _detectionRange = detectionRange;
        _enemyTransform = enemy.GetTargetCenter();

        View = view;
        View.Initialize(_enemyTransform, isBehind => _visibility.SetBehindCamera(isBehind));

        _visibility = new EnemyGaugeVisibilityState(damagedDisplayDuration);
        _visibility.OnVisibilityChanged += View.SetVisible;

        _enemy.OnHealthChanged += HandleHealthChanged;
        // _enemy.OnLockedOnChanged += HandleLockedOnChanged;
    }

    public void SetBehindCamera(bool isBehind)
    {
        _visibility.SetBehindCamera(isBehind);
    }

    /// <summary>距離チェック。EnemyUIManagerのUpdateから呼ぶ</summary>
    public void UpdateRangeCheck()
    {
        if (_playerTransform == null || _enemyTransform == null) return;
        float sqrDist = (_playerTransform.position - _enemyTransform.position).sqrMagnitude;
        _visibility.SetInRange(sqrDist <= _detectionRange * _detectionRange);
    }

    public void ResetView()
    {
        View.ResetView();
    }

    public void Dispose()
    {
        _visibility.OnVisibilityChanged -= View.SetVisible;
        _visibility.Dispose(); // CancellationTokenSourceの破棄
        _enemy.OnHealthChanged -= HandleHealthChanged;
        // _enemy.OnLockedOnChanged -= HandleLockedOnChanged;
    }

    private readonly IEnemy _enemy;
    private readonly Transform _playerTransform;
    private readonly Transform _enemyTransform;
    private readonly float _detectionRange;
    private readonly EnemyGaugeVisibilityState _visibility;

    private void HandleHealthChanged(float current, float max)
    {
        View.UpdateGauge(current, max);
        _visibility.OnDamaged();
    }

    private void HandleLockedOnChanged(bool isLockedOn)
    {
        _visibility.SetLockedOn(isLockedOn);
    }
}
