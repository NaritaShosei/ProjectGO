using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EffectPresenter
{
    private Effect _view;
    private GenericObjectPool<Effect> _pool;
    private HitStopManager _hitStopManager;

    public EffectPresenter(Effect view,GenericObjectPool<Effect> pool,HitStopManager hitStopManager)
    {
        _pool = pool;
        _view = view;
        _hitStopManager = hitStopManager;
    }

    public async UniTask PlayAsync(Vector3 position,Quaternion rotation, CancellationToken ct = default)
    {
        try
        {
            _hitStopManager.Register(_view, HitStopTargetGroup.Effects);
            _view.transform.SetParent(null, false);
            _view.transform.position = position;
            _view.transform.rotation = rotation;

            _view.Play();

            // 再生終了待ち
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
        _view.Cleanup();
        _pool.Release(_view);
        _view = null;
        _hitStopManager.Unregister(_view, HitStopTargetGroup.Effects);
    }
}
