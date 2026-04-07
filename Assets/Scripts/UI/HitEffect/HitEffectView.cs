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

        if(_particleSystem != null)
        {
            _particleSystem.Play();
        }
        Destroy(gameObject,_particleSystem.main.duration);
    }
}
