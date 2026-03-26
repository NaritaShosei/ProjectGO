using UnityEngine;

/// <summary>
/// SpatialHashGridを用いて近隣Enemyへの分離力ベクトルを計算するサービス
/// 距離が近いほど強い反発力を返す（dist / radius で線形減衰）
/// </summary>
public sealed class SeparationService : ISeparationService
{
    /// <summary>
    /// SpatialHashGridへの依存はコンストラクタで注入する
    /// </summary>
    public SeparationService(ISpatialHashGrid grid)
    {
        _grid = grid;
    }

    public Vector3 Calculate(
        IEnemy self,
        Vector3 position,
        float radius,
        float strength
    )
    {
        var neighbors = ListPool<IEnemy>.Get();

        Vector3 force = Vector3.zero;

        try
        {
            _grid.Query(position, radius, neighbors);

            foreach (var other in neighbors)
            {
                if (other == self) continue;

                // (posA - posB) はB→Aのベクトル
                // dist < radius の前提なので、距離が近いほど force が大きい
                Vector3 diff = position - other.GetTargetCenter().position;
                float dist = diff.magnitude;

                if (dist <= 0.001f) continue;

                force += diff.normalized * (1f - dist / radius);
            }
        }
        finally
        {
            ListPool<IEnemy>.Release(neighbors);
        }

        return force * strength;
    }

    private readonly ISpatialHashGrid _grid;
}
