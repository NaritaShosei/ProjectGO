using BossEnemy.AI.BehaviourTree;
using BossEnemy.Character;
using BossEnemy.Enum;
using System;
using UnityEngine;

namespace BossEnemy.Interface
{
    public interface IBossEnemyCharacterController : IUpdater, IDisposable
    {
        /// <summary> 初期化処理 </summary>
        public void Init(
            IBossEnemyCharacterView bossEnemyCharacterView,
            EnemyServices enemyServices,
            IBossCharacterAnimationEventReceiver animationEventReceiver,
            ITreeNode entryNode,
            IBossCharacterEntity bossCharacterEntity);

        /// <summary>
        /// ボスをプールへ返却するため、Entity にデスポーンを通知する。
        /// </summary>
        public void Despawn();
    }
}
