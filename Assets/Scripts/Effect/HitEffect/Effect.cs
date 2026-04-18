using System;
using UnityEngine;
using System.Collections;

public class Effect : MonoBehaviour, ISpeedChange
{
    public float TimeScale { get; set; } = 1f;

    public void Play()
    {
        _particle.Play();
    }

    public void Stop()
    {
        _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public bool IsAlive()
    {
        if (_particles == null) return false;

        foreach (var ps in _particles)
        {
            if (ps.IsAlive(true))
                return true;
        }
        return false;
    }
    public void Cleanup()
    {
        Stop();
    }



    /// <summary>
    /// HitStopから呼ばれる速度変更処理
    /// </summary>
    /// <param name="scale"></param>
    public void OnSpeedChange(float scale)
    {
        TimeScale = scale;
        ApplyTimeScale();
    }

    // メインのParticle
    [SerializeField] private ParticleSystem _particle;
    ////子objectも含めたParticleSystem
    private ParticleSystem[] _particles;
    private void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>();
    }

    /// <summary>
    /// ParticleSysytemにTimeScaleを反映
    /// </summary>
    private void ApplyTimeScale()
    {
        foreach (var ps in _particles)
        {
            var main = ps.main;
            main.simulationSpeed = TimeScale;
        }
    }
}
