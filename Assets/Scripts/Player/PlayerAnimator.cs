using UnityEngine;

public class PlayerAnimator
{
    private Animator _animator;

    public PlayerAnimator(ISpeedManager speedManager, Animator anim)
    {
        speedManager.OnSpeedChanged += ChangeSpeed;
        _animator = anim;
    }

    private void ChangeSpeed(float speed) => _animator.speed = speed;
}