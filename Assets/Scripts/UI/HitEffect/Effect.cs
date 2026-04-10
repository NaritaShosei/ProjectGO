using System;
using UnityEngine;
using System.Collections;

public class Effect : MonoBehaviour
{
    //再生終了時に呼ばれるコールバック
    public Action<Effect> OnFinished;

    public string Key { get; set; }

    // メインのParticle
    [SerializeField] private ParticleSystem _particle;

    //子objectも含めたParticleSystem
    private ParticleSystem[] _particles;


    /// <summary>
    /// エフェクトの再生
    /// </summary>
    public void Play()
    {
        gameObject.SetActive(true);

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

    private Coroutine _coroutine;

    private void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>();
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
