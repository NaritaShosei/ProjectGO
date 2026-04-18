using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EffectPresenter
{
    private Effect _view;
    private GenericObjectPool<Effect> _pool;

    public EffectPresenter(Effect view,GenericObjectPool<Effect> pool)
    {
        _pool = pool;
        _view = view;
    }

    public async UniTask PlayAsync(Vector3 position,Quaternion rotation, CancellationToken ct = default)
    {
        _view.transform.SetParent(null, false);
        _view.transform.position = position;
        _view.transform.rotation = rotation;

        _view.Play();

        // 再生終了待ち
        await UniTask.WaitUntil(() => ct.IsCancellationRequested || !_view.IsAlive(),
    cancellationToken: ct);

        Dispose();
    }

    public void Dispose()
    {
        if (_view == null) return;
        _view.Cleanup();
        _pool.Release(_view);
        _view = null;
    }
}
