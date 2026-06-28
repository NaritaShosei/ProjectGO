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
        if (_cts == null)
        {
            Debug.LogError("[EffectManager] PlayEffect called before initialization.", this);
            return;
        }

        if (!_pools.TryGetValue(key, out var pool))
        {
            Debug.LogError($"Effect not found: {key}", this);
            return;
        }

        var view = pool.Get();
        if (view == null)
        {
            Debug.LogError($"[EffectManager] Effect pool returned null. Key: {key}", this);
            return;
        }

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

        if (_effectDataList == null || _effectDataList.Count == 0)
        {
            Debug.LogWarning("[EffectManager] EffectDataList is empty.", this);
            return;
        }

        foreach (var data in _effectDataList)
        {
            if (string.IsNullOrEmpty(data.Key) || data.Prefab == null)
            {
                Debug.LogWarning("[EffectManager] Invalid EffectData is registered.", this);
                continue;
            }

            if (_pools.ContainsKey(data.Key))
            {
                Debug.LogWarning($"[EffectManager] Duplicate effect key: {data.Key}", this);
                continue;
            }

            var pool = new GenericObjectPool<EffectBase>(data.Prefab, _effectParent, _preloadCount);
            _pools[data.Key] = pool;

            Debug.Log($"Registered Effect: {data.Key}", this);
        }
    }

    private void Start()
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStop))
        {
            _hitStop = hitStop;
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<EffectManager>();

        _cts?.Cancel();
        _cts?.Dispose();
    }
}
