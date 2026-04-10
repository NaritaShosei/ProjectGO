using System;
using UnityEngine;
using System.Collections;

public class Effect : MonoBehaviour
{


    private Coroutine _coroutine;
    public Action<Effect> OnFinished;

    public string Key { get; set; }

    [SerializeField] private ParticleSystem _particle;

    // 子Particleも含めてキャッシュ
    private ParticleSystem[] _particles;

    private void Awake()
    {
        // 子含めて全部取得しておく
        _particles = GetComponentsInChildren<ParticleSystem>();
    }

    public void Play()
    {
        gameObject.SetActive(true);

        // 念のため全部再生（子も確実に）
        foreach (var ps in _particles)
        {
            ps.Play();
        }

        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        // 終了監視スタート
        StartCoroutine(WaitForFinish());
    }

    private IEnumerator WaitForFinish()
    {
        // すべてのParticleが完全停止するまで待つ
        yield return new WaitUntil(() =>
        {
            foreach (var ps in _particles)
            {
                // 1つでも生きてたらまだ終わらない
                if (ps.IsAlive(true))
                    return false;
            }
            return true;
        });

        Finish();
    }

    private void Finish()
    {
        // 念のため停止（再利用時の事故防止）
        foreach (var ps in _particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        gameObject.SetActive(false);

        // Poolに返却通知
        OnFinished?.Invoke(this);
    }
}
