using BossEnemy.Infrastructure.Repository;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace BossEnemy.Attack
{
    public class AttackExecutor : IDisposable
    {
        public AttackExecutor()
        {
            _attackCoolTimer = new();

            // 複数回の攻撃選択から待機されるため、await可能なTaskを保持する。
            _initializationTask = InitAsync().Preserve();
        }

        public AttackData ExecutingAttack => _executingAttackData;

        public bool WasHitAttack => _wasAttackHit;

        /// <summary> 攻撃関連のRepositryLoadのため非同期で初期化 </summary>
        private async UniTask InitAsync()
        {
            _attackDataRepository = await AssetsLoader.LoadAssetAsync<AttackDataRepositry>
                (AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAttackDataRepositry);

            _bossEnemyAttackSelectionPoolRepository = await AssetsLoader.LoadAssetAsync<AttackDataSelectionPoolRepository>
                (AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_AttackDataSelectionPoolRepository);

            _attackDataRepository.Init();
            _bossEnemyAttackSelectionPoolRepository.Init();
        }

        /// <summary> 次の攻撃を確定させる </summary>
        public async UniTask SetNextAttack(int attackSelectPoolID)
        {
            // Addressablesの非同期ロード完了前にAIが攻撃選択へ進まないよう待機する。
            await _initializationTask;

            AttackSelectionPool attackSelectionPool = _bossEnemyAttackSelectionPoolRepository.GetSelectionPool(attackSelectPoolID);

            if (attackSelectionPool.SelectionPool == null) Debug.LogError("PoolがNullです");

            int executeAttackID = AttackDataSelector.GetRandamSelectAttackDataID(attackSelectionPool, _attackCoolTimer.AttackCoolTimeList);

            if(executeAttackID == 0)
            {
                Debug.Log("選択可能な攻撃がありません、CoolTimeを待ちます");

                int awaitFrame = 100;

                await UniTask.Delay(awaitFrame);

                await SetNextAttack(attackSelectPoolID);

                return;
            }

            _executingAttackData = _attackDataRepository.GetData(executeAttackID);
        }

        /// <summary> 攻撃の実行 </summary>
        public void Execute(IPlayer attackTarget)
        {
            _attackTarget = attackTarget;
            _wasAttackHit = false;

            
        }

        /// <summary> 攻撃によってダメージが発生したか否かの判定 </summary>
        public bool TryHitAttack(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default)
        {
            if(AttackHitChecker.TryHitAttack
                (attackHitAreaType, 
                attackPosition, 
                _attackTarget,
                _executingAttackData.AttackHitAreaRadius))
            {
                AttackHit(_attackTarget);
                _wasAttackHit = true;
            }
            else _wasAttackHit = false;

            return _wasAttackHit;
        }

        /// <summary> 攻撃終了 </summary>
        public void Complete()
        {
            _attackCoolTimer.StartCoolTime(_executingAttackData.ID, _executingAttackData.CoolTime).Forget();
            _wasAttackHit = false;
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

        private readonly UniTask _initializationTask;

        private bool _wasAttackHit = false;

        private AttackCoolTimer _attackCoolTimer;

        // 攻撃対象(今のところPlayer1人のみ)
        private IPlayer _attackTarget;

        /// <summary> 攻撃が当たった際のイベント発火時の処理 </summary>
        private void AttackHit(IPlayer hitTarget)
        {
            hitTarget.TakeDamage(_executingAttackData.Damage, _executingAttackData.DamageReaction);
        }
    }
}
