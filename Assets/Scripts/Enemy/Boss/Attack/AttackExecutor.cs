using BossEnemy.Infrastructure.Repository;
using BossEnemy.Interface;
using BossEnemy.Model.System;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static UnityEditorInternal.ReorderableList;

namespace BossEnemy.Attack
{
    public class AttackExecutor : IDisposable
    {
        public AttackExecutor()
        {
            Init().Forget();
        }

        public AttackData ExecutingAttack => _executingAttackData;

        public async UniTask Init()
        {
            _attackDataRepository = await AssetsLoader.LoadAssetAsync<AttackDataRepositry>
            (AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAttackDataRepositry);

            _bossEnemyAttackSelectionPoolRepository = await AssetsLoader.LoadAssetAsync<AttackDataSelectionPoolRepository>
                (AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_AttackDataSelectionPoolRepository);

            _isInit = true;
        }

        public void SetExecuteAttack(int attackSelectPoolID)
        {
            AttackSelectionPool attackSelectionPool = _bossEnemyAttackSelectionPoolRepository.GetSelectionPool(attackSelectPoolID);

            int executeAttackID = AttackDataSelector.GetRandamSelectAttackDataID(attackSelectionPool, _attackCoolTimer.AttackCoolTimeList);

            _executingAttackData = _attackDataRepository.GetData(executeAttackID);
        }

        /// <summary> 攻撃の実行 </summary>
        public void Execute(IPlayer attackTarget)
        {
            _attackTarget = attackTarget;
            _wasAttackHit = false;
        }

        /// <summary> 攻撃によってダメージが発生したか否かの判定 </summary>
        public bool IsHitAttackSuccess(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default)
        {
            switch (attackHitAreaType)
            {
                case AttackHitAreaType.Circle:

                    break;
            }

            return _wasAttackHit;
        }

        /// <summary> 攻撃終了 </summary>
        public void Complete()
        {

        }

        public void Dispose() 
        {
            AssetsLoader.Release(AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAttackDataRepositry);
            AssetsLoader.Release(AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_AttackDataSelectionPoolRepository);
        }

        private AttackData _executingAttackData;

        // 各種攻撃関連リポジトリ
        private IBossEnemyAttackSelectionPoolRepository _bossEnemyAttackSelectionPoolRepository;
        private IBossEnemyAttackDataRepository _attackDataRepository;

        private bool _isInit = false;

        private bool _wasAttackHit = false;

        private AttackCoolTimer _attackCoolTimer;

        // 攻撃対象(今のところPlayer1人のみ)
        private IPlayer _attackTarget;

        /// <summary> 攻撃が当たった際のイベント発火時の処理 </summary>
        private void AttackHit(IPlayer hitTarget)
        {
            hitTarget.TakeDamage(_executingAttackData.Damage);
        }
    }
}
