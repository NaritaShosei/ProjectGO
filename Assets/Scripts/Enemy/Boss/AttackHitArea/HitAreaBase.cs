using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

public abstract class HitAreaBase : MonoBehaviour
{
    public abstract event Action<HitAreaBase, HitAreaType> OnDespawn;

    public abstract UniTask ActiveView();

    public abstract void SetRange(float range);

    public abstract void SetDespawnTime(float despawnTime);

    public abstract void Despawn();
}
