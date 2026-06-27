using UnityEngine;

public interface IAttackHitAreaSpawner
{
    public void Spawn(HitAreaType hitAreaType, Vector3 spawnCenterPos, float range, float despawnTime);
}
