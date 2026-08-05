using UnityEngine;

namespace BossEnemy.Enum
{
    /// <summary> ボスの鎧の装着部位 </summary>
    public enum ArmorAttachmentType
    {
        [InspectorName("装備なし")] None = 0, // default値
        [InspectorName("右腕")] RightArm,
        [InspectorName("左腕")] LeftArm,
        [InspectorName("右足")] RightLeg,
        [InspectorName("左足")] LeftLeg
    }
}
