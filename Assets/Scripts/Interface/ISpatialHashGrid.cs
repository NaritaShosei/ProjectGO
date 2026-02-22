using System.Collections.Generic;
using UnityEngine;

public interface ISpatialHashGrid
{
    /// <summary>
    /// EnemyのGrid位置を登録する
    /// </summary>
    /// <param name="enemy"></param>
    /// <param name="position"></param>
    void Register(IEnemy enemy, Vector3 position);

    /// <summary>
    /// EnemyのGrid位置を更新する
    /// 更新がある場合のみ
    /// </summary>
    /// <param name="enemy"></param>
    /// <param name="oldPos"></param>
    /// <param name="newPos"></param>
    void UpdatePosition(IEnemy enemy, Vector3 oldPos, Vector3 newPos);
    
    /// <summary>
    /// Enemyの登録を解除する
    /// </summary>
    /// <param name="enemy"></param>
    void Remove(IEnemy enemy);

    /// <summary>
    /// Enemy周辺にいる別Enemyのリストを返す
    /// </summary>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    void Query(Vector3 position, float radius, List<IEnemy> result);
}
