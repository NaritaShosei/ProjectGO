using BossEnemy.Effect;
using BossEnemy.Enum;
using UnityEngine;

public interface IAttackHitAreaSpawner
{
    /// <summary> 攻撃範囲エフェクトの生成 </summary>
    /// <param name="hitAreaType"> 当たり判定のタイプ </param>
    /// <param name="spawnCenterPos"> エフェクトの中心座標 </param>
    /// <param name="range"> 当たり判定の半分の大きさ </param>
    /// <param name="despawnTime"> エフェクト消滅までの時間 </param>
    public HitAreaView Spawn(AttackHitAreaType hitAreaType, Vector3 spawnCenterPos, float range, Vector3 forward = default);

    /// <summary>幅と長さが異なる正方形攻撃範囲エフェクトを生成する</summary>
    public HitAreaView SpawnSquare(Vector3 spawnCenterPos, float width, float length, Vector3 forward);
}
