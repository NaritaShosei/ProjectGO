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

        public AttackData NextAttack => _nextAttackData;

        public bool WasHitAttack => _wasAttackHit;

        public UniTask SetNextAttack(int attackSelectPoolID)
        {
            if (_isDisposed) return UniTask.CompletedTask;

            int selectionVersion = ++_selectionVersion;
            return SetNextAttackAsync(attackSelectPoolID, selectionVersion);
        }

        /// <summary> 攻撃の実行 </summary>
        public AttackData ExecuteAttack(IPlayer attackTarget)
        {
            if (attackTarget == null || _nextAttackData.ID == 0) return default;

            _attackTarget = attackTarget;
            _wasAttackHit = false;

            _executingAttackData = _nextAttackData;
            _nextAttackData = default;
            return _executingAttackData;
        }

        /// <summary> 攻撃によってダメージが発生したか否かの判定 </summary>
        public bool TryHitAttack(AttackData tryHitAttackData, AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default)
        {
            if(AttackHitChecker.TryHitAttack
                (attackHitAreaType, 
                attackPosition, 
                _attackTarget,
                tryHitAttackData.AttackHitAreaRadius))
            {
                AttackHit(tryHitAttackData, _attackTarget);
                _wasAttackHit = true;
            }
            else _wasAttackHit = false;

            return _wasAttackHit;
        }

        /// <summary> 攻撃終了 </summary>
        public void AttackCompleted()
        {
            _attackCoolTimer.StartCoolTime(_executingAttackData.ID, _executingAttackData.CoolTime).Forget();
            _wasAttackHit = false;
            _executingAttackData = default;
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            _selectionVersion++; // 待機中の全選択要求を無効化

            ReleaseRepositoriesAfterInitializationAsync().Forget();
        }

        private AttackData _nextAttackData;
        private AttackData _executingAttackData;

        // 各種攻撃関連リポジトリ
        private IBossEnemyAttackSelectionPoolRepository _bossEnemyAttackSelectionPoolRepository;
        private IBossEnemyAttackDataRepository _attackDataRepository;

        private readonly UniTask _initializationTask;

        private bool _wasAttackHit = false;

        private AttackCoolTimer _attackCoolTimer;

        // 攻撃対象(今のところPlayer1人のみ)
        private IPlayer _attackTarget;

        private bool _isDisposed;
        private int _selectionVersion;

        private async UniTask InitAsync()
        {
            var attackDataRepository =
                await AssetsLoader.LoadAssetAsync<AttackDataRepositry>
                (AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAttackDataRepositry);

            if (_isDisposed) return;

            var selectionPoolRepository =
                await AssetsLoader.LoadAssetAsync<AttackDataSelectionPoolRepository>
                (AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_AttackDataSelectionPoolRepository);

            if (_isDisposed) return;

            attackDataRepository.Init();
            selectionPoolRepository.Init();

            if (_isDisposed) return;

            _attackDataRepository = attackDataRepository;
            _bossEnemyAttackSelectionPoolRepository = selectionPoolRepository;
        }

        private async UniTaskVoid ReleaseRepositoriesAfterInitializationAsync()
        {
            try
            {
                await _initializationTask;
            }
            catch (Exception)
            {
                // ロード失敗時も finally で解放する
            }
            finally
            {
                AssetsLoader.Release(AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAttackDataRepositry);
                AssetsLoader.Release(AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_AttackDataSelectionPoolRepository);

                _attackDataRepository = null;
                _bossEnemyAttackSelectionPoolRepository = null;
                _nextAttackData = default;
                _executingAttackData = default;
            }
        }

        private async UniTask SetNextAttackAsync(int attackSelectPoolID, int selectionVersion)
        {
            await _initializationTask;

            if (_isDisposed || selectionVersion != _selectionVersion)
                return;

            var pool = _bossEnemyAttackSelectionPoolRepository.GetSelectionPool(attackSelectPoolID);

            int attackId = AttackDataSelector.GetRandomSelectAttackDataID(
                pool, _attackCoolTimer.AttackCoolTimeList);

            if (attackId == 0)
            {
                await UniTask.Delay(100);

                if (_isDisposed || selectionVersion != _selectionVersion)
                    return;

                await SetNextAttackAsync(attackSelectPoolID, selectionVersion);
                return;
            }

            if (_isDisposed || selectionVersion != _selectionVersion)
                return;

            _nextAttackData = _attackDataRepository.GetData(attackId);
        }

        /// <summary> 攻撃が当たった際のイベント発火時の処理 </summary>
        private void AttackHit(AttackData hitAttack, IPlayer hitTarget)
        {
            hitTarget.TakeDamage(_executingAttackData.Damage, _executingAttackData.DamageReaction);
        }
    }
}
