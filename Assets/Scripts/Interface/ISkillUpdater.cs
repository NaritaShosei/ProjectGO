using UnityEngine;
/// <summary>
/// 一定間隔で使用するスキルにつける
/// </summary>
public interface ISkillUpdater
{
    void OnUpdate(float deltaTime, PlayerMode mode, IPlayerStats stats,Vector3 playerPosition,EnemyManager enemyManager);
}
