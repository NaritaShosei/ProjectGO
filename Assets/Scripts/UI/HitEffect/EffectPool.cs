using System.Collections.Generic;
using UnityEngine;

public class EffectPool : MonoBehaviour
{
    /// <summary>
    /// エフェクト取得
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Effect Get(string key)
    {
        Effect effect;

        if (_pool.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            effect = queue.Dequeue();
        }
        else
        {
            effect = CreateEffect(key);

            if (effect == null) return null;
        }

        _active.Add(effect);

        effect.Key = key;

        effect.OnFinished -= OnEffectFinished;
        effect.OnFinished += OnEffectFinished;

        return effect;
    }

    /// <summary>
    /// エフェクトをプールに変換
    /// </summary>
    /// <param name="effect"></param>
    public void Return(Effect effect)
    {

        if (!_active.Contains(effect))
        {
            return;
        }

        _active.Remove(effect);

        effect.OnFinished -= OnEffectFinished;

        Initialization(effect);

        var key = effect.Key;

        if (!_pool.ContainsKey(key))
        {
            _pool[key] = new Queue<Effect>();

        }
        _pool[key].Enqueue(effect);
    }

    //prefabの一覧
    [SerializeField] private List<Effect> _prefabs;
    //keyのprefab
    private Dictionary<string, Effect> _prefabDic = new();
    //keyごとのPool
    private Dictionary<string, Queue<Effect>> _pool = new();
    //現在使用中のエフェクトの一覧
    private HashSet<Effect> _active = new();

    void Awake()
    {
        foreach (var prefab in _prefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"{nameof(EffectPool)}: _prefabs に null 要素があります。", this);
                continue;
            }
            _prefabDic[prefab.name] = prefab;
        }
    }

    /// <summary>
    /// 新規エフェクトの作成
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private Effect CreateEffect(string key)
    {
        var prefab = GetPrefab(key);
        if (prefab == null) return null;

        var effect = Instantiate(prefab, transform);
        Initialization(effect);
        return effect;
    }

    /// <summary>
    /// prefab取得
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private Effect GetPrefab(string key)
    {
        if (_prefabDic.TryGetValue(key, out var prefab))
        {
            return prefab;
        }

        Debug.LogError($"Prefab not found: {key}");
        return null;
    }

    /// <summary>
    /// エフェクトの状態初期化
    /// </summary>
    /// <param name="effect"></param>
    private void Initialization(Effect effect)
    {
        effect.transform.SetParent(transform, false);

        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        if (effect.TryGetComponent(out ParticleSystem ps))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        effect.gameObject.SetActive(false);
    }

    /// <summary>
    /// Effect側からの終了通知を受け取る
    /// </summary>
    /// <param name="effect"></param>
    private void OnEffectFinished(Effect effect)
    {
        Return(effect);
    }
}
