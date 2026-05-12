using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// エフェクトを管理するマネージャー。
/// 後始末（transform 親戻し・位置リセット）は Effect.OnRelease に委譲したため、
/// onRelease コールバック引数は不要になった。
/// </summary>
public class EffectManager : MonoBehaviour
{
    public void PlayEffect(string key, Vector3 position)
    {
        PlayEffect(key, position, Vector3.one);
    }

    public void PlayEffect(
        string key,
        Vector3 position,
        Vector3 scale)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            Debug.LogError($"Effect not found: {key}");
            return;
        }

        var view = pool.Get();

        view.SetScale(scale);

        var presenter = new EffectPresenter(view, pool, _hitStop);

        presenter.PlayAsync(
            position,
            Quaternion.identity,
            _cts.Token).Forget();
    }

    [SerializeField] private Transform _effectParent;
    [SerializeField] private List<EffectData> _effectDataList;
    [SerializeField] private int _preloadCount = 5;

    private Dictionary<string, GenericObjectPool<EffectBase>> _pools = new();
    private CancellationTokenSource _cts;
    private HitStopManager _hitStop;

    private void Awake()
    {
        ServiceLocator.Register(this);

        _cts = new CancellationTokenSource();

        foreach (var data in _effectDataList)
        {
            if (string.IsNullOrEmpty(data.Key) || data.Prefab == null)
            {
                Debug.LogWarning($"無効な EffectData が登録されています: Key='{data.Key}', Prefab='{data.Prefab}'");
                continue;
            }
            if (_pools.ContainsKey(data.Key))
            {
                Debug.LogWarning($"このキーは既に登録されています: {data.Key}");
                continue;
            }

            var pool = new GenericObjectPool<EffectBase>(data.Prefab, _effectParent, _preloadCount);
            _pools[data.Key] = pool;

            Debug.Log($"Registered Effect: {data.Key}");
        }
    }

    private void Start()
    {
        _hitStop = ServiceLocator.Get<HitStopManager>();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<EffectManager>();

        _cts?.Cancel();
        _cts?.Dispose();
    }
}
