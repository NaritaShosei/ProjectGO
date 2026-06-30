using UnityEngine;

namespace BossEnemy.Enum
{
    public enum PostureType
    {
        [InspectorName("立つ")] Stand,
        [InspectorName("しゃがむ")] Crouch,
        [InspectorName("倒れる")] SpreadEagled
    }

}
