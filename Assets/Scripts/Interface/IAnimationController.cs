using UnityEngine;

public interface IAnimationController
{
    public void AnimEvent_AttackExecute();
    public void AnimEvent_AttackExecute(int hitIndex);
    public void AnimEvent_AttackExecute(int hitIndex, int hitCount);
    public void AnimEvent_AttackComplete();
    public void AnimEvent_ComboWindowStart();
    public void AnimEvent_ComboWindowEnd();
    public void AnimEvent_ModeChangeReady();
    public void AnimEvent_ComboTransition();
}

public interface IModeChangeAnimationController
{
    public void AnimEvent_ModeChangeComplete();
}
