using UnityEngine;

public readonly struct HitEffectContext
{
    public readonly Vector3 Position;
    public readonly PlayerMode PlayerMode;

    public readonly bool IsArmorHit;
    public readonly  bool IsArmorBreak;
}
