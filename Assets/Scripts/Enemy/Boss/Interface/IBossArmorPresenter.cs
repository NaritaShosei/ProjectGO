using UnityEngine;

namespace BossEnemy.Armor
{
    public interface IBossArmorPresenter
    {
        /// <summary> ボスの鎧破壊イベント発火時の処理 </summary>
        public void HandleBreakArmor();

        /// <summary> ボスの鎧修復イベント発火時の処理 </summary>
        public void HandleRepairArmor();
    }
}
