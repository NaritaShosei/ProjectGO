using UnityEngine;

namespace BossEnemy.Enum
{
    /// <summary> ボスエネミーを構成する各パーツの種類 </summary>
    public enum TakeDamageType
    {
        None, // default値
        Hard, // 硬い
        Normal, // そこそこ
        WeekPoint, // 弱点
        VitalPoint // 急所
    }
}
