using UnityEngine;

public interface IHitEffectView
{
    void EffectPlay(Vector3 position);
}

public enum HitEffectType
{
    Warrior,
    Thunder
}
