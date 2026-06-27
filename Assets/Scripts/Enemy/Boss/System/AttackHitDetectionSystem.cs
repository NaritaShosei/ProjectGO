using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public enum HitAreaType
{
    None,
    Circle
}

public class AttackHitDetectionSystem
{
    public static bool TryHitAttack(HitAreaType hitAreaType, Vector3 hitAreaCenterPos, IPlayer target, float hitRange)
    {
        switch (hitAreaType)
        {
            case HitAreaType.Circle:
                target.GetTargetCenter();
                return CircleHitDetect(hitAreaCenterPos, target.GetTargetCenter().position, hitRange);
        }

        return false;
    }

    private static bool CircleHitDetect(Vector3 hitAreaCenterPos, Vector3 targetPos, float hitRange)
    {
        float distance = Vector3.Distance(hitAreaCenterPos, targetPos);
        if (distance <= hitRange) return true;

        return false;
    }
}
