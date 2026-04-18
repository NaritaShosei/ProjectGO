using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public void PlayEffect(string key, Vector3 position)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            Debug.LogError($"Effect not found: {key}");
            return;
        }

        var view = pool.Get();

        var presenter = new EffectPresenter(view, pool,_hitStop);
        presenter.PlayAsync(position, Quaternion.identity, _cts.Token).Forget();
    }

    [SerializeField] private Transform _poolParent;
    [SerializeField] private List<EffectData> _effectDatasLsit;
    private Dictionary<string, GenericObjectPool<Effect>> _pools = new();
    private CancellationTokenSource _cts;
    private HitStopManager _hitStop;


    private void Awake()
    {
        _hitStop = ServiceLocator.Get<HitStopManager>();
        _cts = new CancellationTokenSource();

        foreach (var data in _effectDatasLsit)
        {
            if (string.IsNullOrEmpty(data.Key) || data.Prefab == null)
                continue;
            if (_pools.ContainsKey(data.Key))
                continue;

            var pool = new GenericObjectPool<Effect>(
            data.Prefab,
            _poolParent,
            preloadCount: 5,
            onRelease: e =>
            {
                e.transform.SetParent(_poolParent);
                e.transform.localPosition = Vector3.zero;
                e.transform.localRotation = Quaternion.identity;
            }
        );
            _pools[data.Key] = pool;

            Debug.Log($"Registered Effect: {data.Key}");
        }
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
