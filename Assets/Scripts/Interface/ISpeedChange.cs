using UnityEngine;

public interface ISpeedChange
{
    float TimeScale { get; }
    void OnSpeedChange(float scale);
}
