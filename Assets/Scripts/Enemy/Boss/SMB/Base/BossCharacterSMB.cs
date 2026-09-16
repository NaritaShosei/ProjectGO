using BossEnemy.Infrastructure;
using BossEnemy.Interface;
using System;
using UnityEngine;
using UniRx;

public abstract class BossCharacterSMB : StateMachineBehaviour
{
    public void Init(
            IBossCharacterAnimationEventReceiver bossEnemyAnimationEventReceiver,
            IBossEnemyCharacterView bossEnemyCharacterView,
            Transform bossCharacterTransform)
    {
        _animationEventReceiver = bossEnemyAnimationEventReceiver;
        _bossCharacterView = bossEnemyCharacterView;
        _bossCharacterTransform = bossCharacterTransform;

        _timeScaleDisposable?.Dispose();

        _timeScaleDisposable = _bossCharacterView.TimeScaleReactiveProperty.Subscribe(timeScale =>
        {
            ChangeTimeScale(timeScale);
        });

        _isDisposed = false;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }

    /// <summary>
    /// ボスがプールへ戻るときの後始末を行う。
    /// View から明示的に呼べるようにし、非アクティブ化より前に購読を解除する。
    /// </summary>
    public virtual void Dispose()
    {
        if(_isDisposed) return;

        _timeScaleDisposable?.Dispose();
        _timeScaleDisposable = null;

        _isDisposed = true;
    }

    public void OnDisable() => Dispose();

    protected IBossCharacterAnimationEventReceiver _animationEventReceiver = null;

    protected IBossEnemyCharacterView _bossCharacterView = null;

    protected Transform _bossCharacterTransform;

    protected IDisposable _timeScaleDisposable = null;

    protected float _timeScale = 1.0f;

    private bool _isDisposed = true;

    /// <summary> ボスの音源再生メソッド </summary>
    protected virtual void PlayBossSE(string cueName)
    {
        if (string.IsNullOrEmpty(cueName)) return;
        if (_bossCharacterTransform == null) return;

        // 再生
        Sound.PlaySE(_bossCharacterTransform.gameObject, cueName, CueSheetType.Boss);
    }

    protected virtual void ChangeTimeScale(float timeScale)
    {
        _timeScale = timeScale;
    }
}
