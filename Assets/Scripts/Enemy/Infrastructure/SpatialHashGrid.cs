using System.Collections.Generic;
using UnityEngine;

public class SpatialHashGrid : ISpatialHashGrid
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="cellSize">Gridの1辺</param>
    public SpatialHashGrid(float cellSize)
    {
        // 最低限0.1f以上とした。
        this.cellSize = Mathf.Max(cellSize, 0.1f);
    }

    public void Register(IEnemy enemy, Vector3 position)
    {
        var cell = WorldToCell(position);

        // 登録先Gridが作成済みか調べ、なければ作る。
        if (!enemiesInGrid.TryGetValue(cell, out var list))
        {
            // 初期枠8とする
            list = new List<IEnemy>(8);
            enemiesInGrid[cell] = list;
        }
        list.Add(enemy);

        // Enemy-Gridの登録
        enemyGridMap[enemy] = cell;
    }

    public void UpdatePosition(IEnemy enemy, Vector3 oldPos, Vector3 newPos)
    {
        var oldCell = WorldToCell(oldPos);
        var newCell = WorldToCell(newPos);

        // Gridをまたいでいなければリターン
        if (oldCell == newCell) return;


        // 前グリッドから自分を登録解除
        if (enemiesInGrid.TryGetValue(oldCell, out var oldList))
            oldList.Remove(enemy);

        // 次のグリッドに登録
        Register(enemy, newPos);
    }

    public void Remove(IEnemy enemy)
    {
        // Listに自分の名前がなかったらリターン
        if (!enemyGridMap.TryGetValue(enemy, out var cell)) return;

        // Gridの登録, Enemyのリストそれぞれから削除
        enemiesInGrid[cell].Remove(enemy);
        enemyGridMap.Remove(enemy);
    }

    public void Query(Vector3 position, float radius, List<IEnemy> result)
    {
        // どれくらい近いGridまで取得するか
        int range = Mathf.CeilToInt(radius / cellSize);

        var center = WorldToCell(position);

        // 対象Gridひとつずつ調査
        for (int x = -range; x <= range; x++)
        for (int z = -range; z <= range; z++)
        {
            var cell = new Vector3Int(center.x + x, 0, center.z + z);
            if (!enemiesInGrid.TryGetValue(cell, out var list)) continue;

            foreach (var enemy in list)
            {
                // 対象Enemyが確かにradius内にいるか
                if ((enemy.GetTargetCenter().position - position).sqrMagnitude <= radius * radius)
                    result.Add(enemy);
            }
        }
    }

    private readonly float cellSize;
    
    // GridにどのEnemyがいるか
    private readonly Dictionary<Vector3Int, List<IEnemy>> enemiesInGrid = new();
    
    // EnemyがどのGridにいるか
    private readonly Dictionary<IEnemy, Vector3Int> enemyGridMap = new();
    
    /// <summary>
    /// 座標変換
    /// 高さを意識しない
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    Vector3Int WorldToCell(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(pos.x / cellSize),
            0,
            Mathf.FloorToInt(pos.z / cellSize)
        );
    }
}
