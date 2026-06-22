using UnityEngine;

public interface IBossEnemyAnimationController : IEnemyAnimationController
{
    void AnimEvent_BossPhaseChangeEnd();
}
