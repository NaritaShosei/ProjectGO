using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class LockOnMarkerPresenter : IDisposable
{
    public LockOnMarkerPresenter(
        CameraManager cameraManager,
        LockOnMarkerView view,
        CancellationToken token)
    {
        _cameraManager = cameraManager;
        _view = view;

        _view.SetCamera(_cameraManager.MainCamera);

        _cameraManager.OnLockOnTargetChanged += HandleLockOnTargetChanged;

        TickAsync(token).Forget();
    }

    public void Dispose()
    {
        _cameraManager.OnLockOnTargetChanged -= HandleLockOnTargetChanged;
    }

    private readonly CameraManager _cameraManager;
    private readonly LockOnMarkerView _view;
    private ILockOnTarget _currentTarget;

    private void HandleLockOnTargetChanged(ILockOnTarget target)
    {
        _currentTarget = target;

        if (target == null || target.GetTargetCenter() == null)
        {
            _view.Hide();
            return;
        }

        _view.Show(target.GetTargetCenter().position);
    }

    private async UniTask TickAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                Tick();
                await UniTask.Yield(PlayerLoopTiming.LastUpdate, token);
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は正常終了
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void Tick()
    {
        // ロックオン中は毎フレーム位置を更新する
        if (_currentTarget == null) return;

        var center = _currentTarget.GetTargetCenter();
        if (center == null) return;

        _view.UpdatePosition(center.position);
    }
}
