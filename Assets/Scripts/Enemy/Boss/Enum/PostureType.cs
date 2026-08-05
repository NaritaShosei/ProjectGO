using UnityEngine;

namespace BossEnemy.Enum
{
    /// <summary> BossEnemyがアイドル状態の時の姿勢 </summary>
    public enum PostureType
    {
        [InspectorName("設定されていません")] None = 0, // default値
        [InspectorName("立つ")] Stand,
        [InspectorName("しゃがむ")] Crouch,
        [InspectorName("倒れる")] SpreadEagled
    }
}
