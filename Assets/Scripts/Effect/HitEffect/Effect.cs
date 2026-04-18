using System;
using UnityEngine;
using System.Collections;

public class Effect : MonoBehaviour, ISpeedChange
{
    //再生終了時に呼ばれるコールバック
    public Action<Effect> OnFinished;
    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// エフェクトの再生
    /// </summary>
    public void Play()
    {
        gameObject.SetActive(true);

        ApplyTimeScale();

        foreach (var ps in _particles)
        {
            ps.Play();
        }

        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
        //再生終了を監視
        _coroutine = StartCoroutine(WaitForFinish());
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
    //子objectも含めたParticleSystem
    private ParticleSystem[] _particles;

    private Coroutine _coroutine;
    private HitStopManager _hitStopManager;

    private void OnEnable()
    {
        if (ServiceLocator.TryGet<HitStopManager>(out var manager))
        {
            _hitStopManager = manager;
            _hitStopManager?.Register(this, HitStopTargetGroup.Effects);
        }
    }

    private void OnDisable()
    {
        _hitStopManager.Unregister(this, HitStopTargetGroup.Effects);
    }
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

    /// <summary>
    /// すべてのParticleSystemが終了するまで待機
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForFinish()
    {
        yield return new WaitUntil(() =>
        {
            foreach (var ps in _particles)
            {
                if (ps.IsAlive(true))
                    return false;
            }
            return true;
        });

        Finish();
    }

    /// <summary>
    /// エフェクトの終了処理
    /// </summary>
    private void Finish()
    {
        foreach (var ps in _particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        gameObject.SetActive(false);

        OnFinished?.Invoke(this);
    }
}
