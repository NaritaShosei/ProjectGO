using UnityEngine;

public interface ISpeedChange
{
    float TimeScale { get; set; }
    void OnSpeedChange(float scale);
}
