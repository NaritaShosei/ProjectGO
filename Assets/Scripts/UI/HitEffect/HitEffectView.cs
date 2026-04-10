using UnityEngine;

/// <summary>
/// ヒットエフェクトView
/// </summary>
public class HitEffectView : MonoBehaviour,IHitEffectView
{
    [SerializeField]private ParticleSystem _particleSystem;

    public void EffectPlay(Vector3 position)
    {
        transform.position = position;

        float destroyDelay = 0f;

        if (_particleSystem != null)
        {
            _particleSystem.Play();
            destroyDelay = _particleSystem.main.duration;
        }
        Destroy(gameObject,destroyDelay);
    }
}
