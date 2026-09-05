using UnityEngine;

namespace BossEnemy.Model.System
{
    public class AttackHitChecker
    {
        public static bool TryHitAttack(AttackHitAreaType hitAreaType, Vector3 hitAreaCenterPos, IPlayer target, float hitRange, Vector3 forward = default)
        {
            switch (hitAreaType)
            {
                case AttackHitAreaType.Circle:
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

}
