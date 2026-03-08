using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// ゲージの表示状態を管理するクラス
/// 複数の表示条件をOR管理し、いずれか1つでも真なら表示
/// </summary>
public class EnemyGaugeVisibilityState : IDisposable
{
    public event Action<bool> OnVisibilityChanged;
    public bool IsVisible => (_isInRange || _isLockedOn || _isDamagedTimerActive)
                             && (!_isBehindCamera || _isDamagedTimerActive);

    public EnemyGaugeVisibilityState(float damagedDisplayDuration)
    {
        _damagedDisplayDuration = damagedDisplayDuration;
    }

    public void SetInRange(bool inRange)
    {
        if (_isInRange == inRange) { return; }
        _isInRange = inRange;
        NotifyIfChanged();
    }

    public void SetLockedOn(bool lockedOn)
    {
        if (_isLockedOn == lockedOn) { return; }
        _isLockedOn = lockedOn;
        NotifyIfChanged();
    }

    public void SetBehindCamera(bool isBehind)
    {
        if (_isBehindCamera == isBehind) return;
        _isBehindCamera = isBehind;
        NotifyIfChanged();
    }

    /// <summary>攻撃を受けたときに呼ぶ。タイマーをキャンセルして再スタート</summary>
    public void OnDamaged()
    {
        // 既存タイマーをキャンセルして再スタート
        CancelDamagedTimer();
        _damagedCts = new CancellationTokenSource();
        StartDamagedTimerAsync(_damagedCts.Token).Forget();
    }

    public void Dispose()
    {
        CancelDamagedTimer();
        OnVisibilityChanged = null;
    }

    private bool _isInRange;
    private bool _isLockedOn;
    private bool _isBehindCamera;
    private bool _isDamagedTimerActive;
    private float _damagedDisplayDuration;

    private CancellationTokenSource _damagedCts;

    private async UniTaskVoid StartDamagedTimerAsync(CancellationToken ct)
    {
        bool wasVisible = IsVisible;
        _isDamagedTimerActive = true;
        if (!wasVisible) NotifyIfChanged();

        await UniTask.Delay(
            TimeSpan.FromSeconds(_damagedDisplayDuration),
            cancellationToken: ct
        );

        _isDamagedTimerActive = false;
        NotifyIfChanged();
    }

    private void CancelDamagedTimer()
    {
        if (_damagedCts == null) { return; }
        _damagedCts.Cancel();
        _damagedCts.Dispose();
        _damagedCts = null;
        _isDamagedTimerActive = false;
    }

    private void NotifyIfChanged()
    {
        OnVisibilityChanged?.Invoke(IsVisible);
    }
}
