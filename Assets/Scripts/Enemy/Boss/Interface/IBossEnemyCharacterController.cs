using BossEnemy.AI.BehaviourTree;
using BossEnemy.Character;
using BossEnemy.Enum;
using UnityEngine;

namespace BossEnemy.Interface
{
    public interface IBossEnemyCharacterController : IUpdater
    {
        /// <summary> 初期化処理 </summary>
        public void Init(
            IBossEnemyCharacterView bossEnemyCharacterView,
            EnemyServices enemyServices,
            IBossCharacterAnimationEventReceiver animationEventReceiver,
            ITreeNode entryNode,
            IBossCharacterEntity bossCharacterEntity);
    }
}
