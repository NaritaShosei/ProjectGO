using System;
using UnityEngine;

public class PlayerAnimator : IDisposable
{
    private Animator _animator;
    private ISpeedManager _speedManager;
    public PlayerAnimator(ISpeedManager speedManager, Animator anim)
    {
        _speedManager = speedManager;
        _speedManager.OnSpeedChanged += ChangeSpeed;
        _animator = anim;
    }

    public void Dispose()
    {
        if (_speedManager is null) { return; }

        _speedManager.OnSpeedChanged -= ChangeSpeed;
    }

    private void ChangeSpeed(float speed) => _animator.speed = speed;
}