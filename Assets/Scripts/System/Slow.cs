using System;
using UnityEngine;

[Serializable]
public class Slow
{
    public void Init(ISpeedManager speedManager)
    {
        _manager = speedManager;
    }

    public float Value => _isSlow ? _slowSpeed : 1;
    public float SlowDuration => _slowDuration;

    public void ChangeSlow(bool value)
    {
        _isSlow = value;
        _manager.UpdateSpeed();
    }

    [SerializeField] private float _slowSpeed = 0.5f;
    [SerializeField] private float _slowDuration = 0.5f;

    private ISpeedManager _manager;
    private bool _isSlow;
}
