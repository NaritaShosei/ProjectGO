using UnityEngine;

public interface IAttackHitAreaSpawner
{
    /// <summary> 攻撃範囲エフェクトの生成 </summary>
    /// <param name="hitAreaType"> 当たり判定のタイプ </param>
    /// <param name="spawnCenterPos"> エフェクトの中心座標 </param>
    /// <param name="range"> 当たり判定の半分の大きさ </param>
    /// <param name="despawnTime"> エフェクト消滅までの時間 </param>
    public void Spawn(HitAreaType hitAreaType, Vector3 spawnCenterPos, float range, float despawnTime);
}
