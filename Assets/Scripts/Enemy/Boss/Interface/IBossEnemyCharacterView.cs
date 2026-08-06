using System;
using System.Collections.Generic;
using UnityEngine;
using BossEnemy.Character;

namespace BossEnemy.Interface
{
    public interface IBossEnemyCharacterView : IEnemy, IPoolable, ISpeedChange
    {
        /// <summary> ロックオン可能なパーツが変わった際のイベント(新しいターゲット、古いターゲット) </summary>
        public event Action<(IReadOnlyList<ILockOnTarget> newTargetParts, IReadOnlyList<ILockOnTarget> oldTargetParts)> OnChangeLockOnParts;

        /// <summary> 現在攻撃可能なボスの部位 </summary>
        public BossEnemyPartsView[] ActiveBossEnemyPartsView { get; }

        public void SetVelocity(Vector3 velocity);
    }
}
