using Boss.AI.BehaviourTree;
using Boss.Character;
using Boss.Enum;
using System;
using UnityEngine;

namespace Boss.Interface
{
    public interface IBossEnemyCharacterController : IUpdater, IDisposable
    {
        /// <summary> 初期化処理 </summary>
        public void Init(
            IBossEnemyCharacterView bossEnemyCharacterView,
            EnemyServices enemyServices,
            IBossCharacterAnimationEventReceiver animationEventReceiver,
            TreeNode entryNode,
            IBossCharacterEntity bossCharacterEntity);

        /// <summary>
        /// ボスをプールへ返却するため、Entity にデスポーンを通知する。
        /// </summary>
        public void Despawn();
    }
}
