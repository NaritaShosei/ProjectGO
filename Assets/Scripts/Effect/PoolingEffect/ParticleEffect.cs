using UnityEngine;

public class ParticleEffect : EffectBase
{
    [SerializeField] private ParticleSystem _rootParticle;
    private ParticleSystem[] _particles;

    protected override void ApplyTimeScaleInternal(float scale)
    {
        if (_particles == null) return;
        foreach (var ps in _particles)
        {
            if (ps == null) continue;
            var main = ps.main;
            main.simulationSpeed = scale;
        }
    }

    protected override bool IsAliveInternal()
    {
        if (_particles == null) return false;
        foreach (var ps in _particles)
        {
            if (ps != null && ps.IsAlive(true))
                return true;
        }
        return false;
    }

    protected override void OnPlayInternal()
    {
        if (_rootParticle != null)
            _rootParticle.Play();
    }

    protected override void OnStopInternal()
    {
        if (_rootParticle != null)
            _rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    protected override void ApplyScaleInternal(Vector3 scale)
    {
        transform.localScale = scale;
    }
    protected override void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
    }
}
