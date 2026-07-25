using UnityEngine;

public interface IEnemySpawnState
{
    /// <summary>スポーン時状態を切り替える</summary>
    void SetSpawnState(bool active);
    bool CanTakeDamage { get; }
    bool CanReceiveCondition { get; }
}
