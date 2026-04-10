using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [SerializeField] private EffectPool _pool;

    public void PlayEffect(string key, Vector3 position)//string key をenum化したい（string卒業）
    {
        var effect = _pool.Get(key);
        if (effect == null) return;

        effect.transform.SetParent(null);

        effect.transform.position = position;
        effect.transform.rotation = Quaternion.identity;

        effect.Play();
    }

    public void PlayEffect(string key, Transform trasfomr)
    {
        var effect = _pool.Get(key);
        if (effect == null) return;

        effect.transform.SetParent(trasfomr);

        effect.transform.position = Vector3.zero;
        effect.transform.rotation = Quaternion.identity;

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
}
