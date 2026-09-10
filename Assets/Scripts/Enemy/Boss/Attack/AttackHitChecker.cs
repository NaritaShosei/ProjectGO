using UnityEngine;

using BossEnemy.Enum;

namespace BossEnemy.Attack
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
            // 円形範囲表示は地面（XZ 平面）に描画されるため、実判定も同じ平面で測る。
            Vector3 offset = targetPos - hitAreaCenterPos;
            offset.y = 0f;
            return offset.sqrMagnitude <= hitRange * hitRange;
        }
    }

}
