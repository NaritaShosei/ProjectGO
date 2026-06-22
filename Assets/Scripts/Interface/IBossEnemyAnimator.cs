using UnityEngine;

public interface IBossEnemyAnimator : IEnemyAnimator
{
    public void SetAttackTrigger(string triggerName);
}
