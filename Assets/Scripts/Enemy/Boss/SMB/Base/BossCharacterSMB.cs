using BossEnemy.Infrastructure;
using BossEnemy.Interface;
using UnityEngine;

public abstract class BossCharacterSMB : StateMachineBehaviour
{
    public void Init(
            IBossCharacterAnimationEventReceiver bossEnemyAnimationEventReceiver,
            Transform bossCharacterTransform)
    {
        _animationEventReceiver = bossEnemyAnimationEventReceiver;
        _bossCharacterTransform = bossCharacterTransform;
    }

    protected IBossCharacterAnimationEventReceiver _animationEventReceiver = null;

    protected Transform _bossCharacterTransform;
}
