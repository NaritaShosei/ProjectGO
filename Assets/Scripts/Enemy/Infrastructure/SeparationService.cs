using UnityEngine;

public class SeparationService : ISeparationService
{

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="grid"></param>
    public SeparationService(ISpatialHashGrid grid)
    {
        this.grid = grid;
    }

    public Vector3 Calculate(
        IEnemy self,
        Vector3 position,
        float radius,
        float strength
    )
    {
        // Poolを借りる
        var neighbors = EnemyListPool<IEnemy>.Get();

        // Gridから近隣情報取得
        grid.Query(position, radius, neighbors);
        Vector3 force = Vector3.zero;

        foreach (var other in neighbors)
        {
            // 自分ならリターン
            if (other == self) continue;

            // ベクトル計算
            // イプシロン制限あり
            // (posA - posB)はB→Aのベクトル
            // 前提としてdist < radiusなので、距離が近いほどforceが大きい
            Vector3 diff = position - other.GetTargetCenter().position;
            float dist = diff.magnitude;

            // TODO: もしイプシロンをほかでも使用するなら定数化する？
            if (dist <= 0.001f) continue;

            force += diff.normalized * (1f - dist / radius);
        }

        // 忘れずにPoolを返す
        EnemyListPool<IEnemy>.Release(neighbors);

        return force * strength;
    }

    private readonly ISpatialHashGrid grid;

}
