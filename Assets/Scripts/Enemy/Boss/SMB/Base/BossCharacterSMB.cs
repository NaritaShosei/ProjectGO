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

        _timeScaleDisposable = _bossCharacterView.TimeScaleReactiveProperty.Subscribe(timeScale =>
        {
            ChangeTimeScale(timeScale);
        });
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }

    public void OnDisable()
    {
        _timeScaleDisposable?.Dispose();
        _timeScaleDisposable = null;
    }

    protected IBossCharacterAnimationEventReceiver _animationEventReceiver = null;

    protected IBossEnemyCharacterView _bossCharacterView = null;

    protected Transform _bossCharacterTransform;

    protected IDisposable _timeScaleDisposable = null;

    protected float _timeScale = 1.0f;

    protected virtual void ChangeTimeScale(float timeScale)
    {
        _timeScale = timeScale;
    }
}
