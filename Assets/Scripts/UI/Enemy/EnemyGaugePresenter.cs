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

        _visibility = new EnemyGaugeVisibilityState(damagedDisplayDuration);

        View = view;
        View.Initialize(_enemyTransform, isBehind => _visibility.SetBehindCamera(isBehind));

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

public class ArmorGaugePresenter : IDisposable
{
    public EnemyGaugeView View { get; }

    public event Action<ArmorGaugePresenter> OnBroken;

    public ArmorGaugePresenter(
        IArmorHealth armor,
        EnemyGaugeView view,
        Transform gaugeTarget,
        Transform playerTransform,
        float detectionRange,
        float damagedDisplayDuration)
    {
        _armor = armor;
        _playerTransform = playerTransform;
        _detectionRange = detectionRange;
        // HPゲージと同じEnemy基準位置を使い、2本のゲージを重ねて表示する。
        _gaugeTarget = gaugeTarget;

        _visibility = new EnemyGaugeVisibilityState(damagedDisplayDuration);

        View = view;
        View.Initialize(_gaugeTarget, isBehind => _visibility.SetBehindCamera(isBehind));

        _visibility.OnVisibilityChanged += View.SetVisible;
        _armor.OnHealthChanged += HandleHealthChanged;
        _armor.OnBroken += HandleBroken;
    }

    public void UpdateRangeCheck()
    {
        if (_playerTransform == null || _gaugeTarget == null) return;
        float sqrDist = (_playerTransform.position - _gaugeTarget.position).sqrMagnitude;
        _visibility.SetInRange(sqrDist <= _detectionRange * _detectionRange);
    }

    public void ResetView() => View.ResetView();

    public void Dispose()
    {
        _visibility.OnVisibilityChanged -= View.SetVisible;
        _visibility.Dispose();
        _armor.OnHealthChanged -= HandleHealthChanged;
        _armor.OnBroken -= HandleBroken;
    }

    private readonly IArmorHealth _armor;
    private readonly Transform _playerTransform;
    private readonly Transform _gaugeTarget;
    private readonly float _detectionRange;
    private readonly EnemyGaugeVisibilityState _visibility;

    private void HandleHealthChanged(float current, float max)
    {
        View.UpdateGauge(current, max);
        _visibility.OnDamaged();
    }
    private void HandleBroken()
    {
        OnBroken?.Invoke(this);
    }

}
