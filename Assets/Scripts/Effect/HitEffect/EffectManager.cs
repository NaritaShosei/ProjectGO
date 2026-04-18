using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public void PlayEffect(string key, Vector3 position)
    {
        var effect = _pool.Get(key);
        if (effect == null) return;

        effect.transform.SetParent(null);
        effect.transform.position = position;
        effect.transform.rotation = Quaternion.identity;

        effect.Play();
    }

    public void PlayEffect(string key, Transform transform)
    {
        var effect = _pool.Get(key);
        if (effect == null) return;

        effect.transform.SetParent(transform,false);

        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        effect.Play();
    }

    public void PlayEffect(string key, Vector3 position,Quaternion rotation)
    {
        var effect = _pool.Get(key);
        if (effect == null) return;

        effect.transform.SetParent(null);

        effect.transform.position = position;
        effect.transform.rotation = rotation;

        effect.Play();
    }

    public void PlayEffect(string key, Transform parent, Vector3 localPosition)
    {
        var effect = _pool.Get(key);
        if (effect == null) return;

        effect.transform.SetParent(parent);

        effect.transform.localPosition = localPosition;
        effect.transform.localRotation = Quaternion.identity;

        effect.Play();
    }

    [SerializeField] private EffectPool _pool;

    void Awake()
    {
        if(_pool == null )
        {
            Debug.LogError($"{nameof(EffectManager)}: _pool が未設定です。", this);
            enabled = false;
        }
    }
}
