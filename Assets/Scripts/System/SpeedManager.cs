using System;
using UnityEngine;

public class SpeedManager : MonoBehaviour, ISpeedManager
{
    [SerializeField] private Slow _slow;

    public event Action<float> OnSpeedChanged;
    public Slow Slow => _slow;

    private float _speed = 1;

    private void Awake()
    {
        _slow.Init(this);
    }

    public void UpdateSpeed()
    {
        float newSpeed = _speed * _slow.Value;
        OnSpeedChanged?.Invoke(newSpeed);
    }
}