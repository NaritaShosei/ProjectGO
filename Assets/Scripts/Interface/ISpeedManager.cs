using System;

public interface ISpeedManager
{
    public event Action<float> OnSpeedChanged;
    public void UpdateSpeed();
}
