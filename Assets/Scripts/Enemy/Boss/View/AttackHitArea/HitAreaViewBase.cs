using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

public abstract class HitAreaViewBase : MonoBehaviour
{
    public abstract event Action<HitAreaViewBase, HitAreaType> OnDespawn;

    public abstract void ActiveView(float range, float despawnTime);

    public abstract void SetRange(float range);

    public abstract void SetDespawnTime(float despawnTime);

    public abstract void Despawn();
}
