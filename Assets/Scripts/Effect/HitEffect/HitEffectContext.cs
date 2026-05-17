using UnityEngine;

public struct HitEffectContext
{
    public Vector3 Position;
    public PlayerMode PlayerMode;

    public bool IsWeakPoint;
    public bool IsCritical;
    public bool IsArmorHit;
    public bool IsArmorBreak;
}
