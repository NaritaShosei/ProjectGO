using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EffectPresenter
{
    public EffectPresenter(EffectBase view, GenericObjectPool<EffectBase> pool, HitStopManager hitStopManager)
    {
        _pool = pool;
        _view = view;
        _hitStopManager = hitStopManager;
    }

    public async UniTask PlayAsync(Vector3 position, Quaternion rotation, CancellationToken ct = default)
    {
        try
        {
            _view.transform.SetParent(null, false);
            _view.transform.position = position;
            _view.transform.rotation = rotation;
            if (_view.IsSpeedChangeEnabled)
            {
                _hitStopManager?.Register(_view, HitStopTargetGroup.Effects);
            }

            _view.Play();

            await UniTask.WaitUntil(() => ct.IsCancellationRequested || !_view.IsAlive(), cancellationToken: ct);
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_view == null) return;
        _hitStopManager?.Unregister(_view, HitStopTargetGroup.Effects);
        // OnRelease（後始末）は pool.Release 内で IPoolable.OnRelease として呼ばれる
        _pool.Release(_view);
        _view = null;
    }

    private EffectBase _view;
    private GenericObjectPool<EffectBase> _pool;
    private HitStopManager _hitStopManager;
}
