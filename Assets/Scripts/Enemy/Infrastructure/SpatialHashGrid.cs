using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// XZ平面を均等なセルに分割し、近隣Enemy検索をO(1)に近づける空間ハッシュグリッド
/// Y方向は無視し、水平面上の2D近傍クエリに特化している
/// </summary>
public sealed class SpatialHashGrid : ISpatialHashGrid
{
    /// <summary>
    /// セルサイズはコンストラクタで指定する（最小0.1f）
    /// </summary>
    public SpatialHashGrid(float cellSize)
    {
        _cellSize = Mathf.Max(cellSize, 0.1f);
    }

    public void Register(IEnemy enemy, Vector3 position)
    {
        var cell = WorldToCell(position);

        if (!_enemiesInGrid.TryGetValue(cell, out var list))
        {
            list = new List<IEnemy>(8);
            _enemiesInGrid[cell] = list;
        }

        list.Add(enemy);
        _enemyGridMap[enemy] = cell;
    }

    public void UpdatePosition(IEnemy enemy, Vector3 oldPos, Vector3 newPos)
    {
        var oldCell = WorldToCell(oldPos);
        var newCell = WorldToCell(newPos);

        if (oldCell == newCell) return;

        if (_enemiesInGrid.TryGetValue(oldCell, out var oldList))
            oldList.Remove(enemy);

        Register(enemy, newPos);
    }

    public void Remove(IEnemy enemy)
    {
        if (!_enemyGridMap.TryGetValue(enemy, out var cell)) return;

        _enemiesInGrid[cell].Remove(enemy);
        _enemyGridMap.Remove(enemy);
    }

    public void Query(Vector3 position, float radius, List<IEnemy> result)
    {
        int range = Mathf.CeilToInt(radius / _cellSize);
        var center = WorldToCell(position);

        for (int x = -range; x <= range; x++)
            for (int z = -range; z <= range; z++)
            {
                var cell = new Vector3Int(center.x + x, 0, center.z + z);
                if (!_enemiesInGrid.TryGetValue(cell, out var list)) continue;

                // 逆順ループでRemove中のインデックスずれを防ぐ
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var enemy = list[i];

                    if (enemy is Object enemyObject && enemyObject == null)
                    {
                        list.RemoveAt(i);
                        _enemyGridMap.Remove(enemy);
                        continue;
                    }

                    // GetTargetCenter()が破棄済みの場合はグリッドから除去してスキップ
                    var targetCenter = enemy.GetTargetCenter();
                    if (targetCenter == null)
                    {
                        list.RemoveAt(i);
                        _enemyGridMap.Remove(enemy);
                        continue;
                    }

                    if ((targetCenter.position - position).sqrMagnitude <= radius * radius)
                        result.Add(enemy);
                }
            }
    }

    private readonly float _cellSize;

    // セル座標 → そのセル内のEnemy一覧
    private readonly Dictionary<Vector3Int, List<IEnemy>> _enemiesInGrid = new();

    // Enemy → 現在いるセル座標
    private readonly Dictionary<IEnemy, Vector3Int> _enemyGridMap = new();

    /// <summary>
    /// ワールド座標をセル座標に変換する（Y軸は無視）
    /// </summary>
    private Vector3Int WorldToCell(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(pos.x / _cellSize),
            0,
            Mathf.FloorToInt(pos.z / _cellSize)
        );
    }
}
