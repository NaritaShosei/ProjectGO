using System;
using UnityEngine;

public class Effect : MonoBehaviour
{
    private string _key;

    [SerializeField] private ParticleSystem _particle;
    public Action<Effect> OnFinishd;
    public void Iint(EffectPool pool,string key)
    {
        _key = key;
    }

    public void Play()
    {
        gameObject.SetActive(true);
        if (_particle != null)
        {
            _particle.Play();
        }
    }
    public void Finish()
    {
        gameObject.SetActive(false);

        OnFinishd?.Invoke(this);
    }
}
