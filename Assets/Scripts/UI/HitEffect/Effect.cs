using System;
using UnityEngine;

public class Effect : MonoBehaviour
{
    public Action<Effect> OnFinished;
    public string Key { get; set; }
    public void Play()
    {
        gameObject.SetActive(true);
        if (_particle != null)
        {
            _particle.Play();
        }
    }

    [SerializeField] private ParticleSystem _particle;

    void Awake()
    {
        if(_particle != null)
        {
            var main = _particle.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    private void OnParticleSystemStopped()
    {
        Finish();
    }

    private void Finish()
    {
        gameObject.SetActive(false);
        OnFinished?.Invoke(this);
    }
}
