using BossEnemy.AI.BehaviourTree;
using BossEnemy.Enum;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace BossEnemy.Character
{
    public class BossCharacterController : IBossEnemyCharacterController 
    {
        public void Init(
            IBossEnemyCharacterView bossEnemyCharacterView,
            EnemyServices enemyServices,
            IBossCharacterAnimationEventReceiver animationEventReceiver,
            ITreeNode entryNode,
            IBossCharacterEntity bossCharacterEntity)
        {
            if(bossCharacterEntity  == null) Debug.LogWarning("null");

            // 必要なオブジェクトを取得、初期化
            _bossCharacterView = bossEnemyCharacterView;
            _enemyServices = enemyServices;
            _animationEventReceiver = animationEventReceiver;
            _bossAIBehaviourController = new(entryNode);
            _nodeRunningConditionNotifier = entryNode.NodeRunningConditionNotifier;
            _characterEntity = bossCharacterEntity;

            RegisterEvents();

            _isDispose = false;
            _isDespawnNotified = false;
        }

        public void Dispose()
        {
            if(_isDispose) return;

            if (_characterEntity.ExecutingAttackData.ID != 0)
                _characterEntity.CancelAttack();

            _bossAIBehaviourController.StopRunning();
            _bossAIBehaviourController.Dispose();
            UnregisterEvents();

            _isDispose = true;
        }

        /// <summary>
        /// BossBattleState の終了処理から呼ばれる、通常のボス回収入口。
        /// Disposeと異なり、Spawnerがプール返却を行えるようEntityに通知する。
        /// </summary>
        public void Despawn()
        {
            // 死亡処理でDispose済みでも、Spawnerへのプール返却通知は必要。
            // そのため、通知済みかどうかはDispose状態とは別に管理する。
            if (_isDespawnNotified) return;

            if (!_isDispose)
                Dispose();

            if (_characterEntity == null) return;

            // OnDespawnCompletedの購読からView.OnReleaseが同期的に呼ばれても
            // 二重で通知しないよう、破棄処理より先に記録する。
            _isDespawnNotified = true;

            Debug.Log("デスポーン");

            // Entityを完全に破棄してからOnDespawnCompletedでSpawnerに返却を通知する。
            _characterEntity.OnDespawn();
        }

        public void OnUpdate()
        {
            if (_bossAIBehaviourController != null)
                _bossAIBehaviourController.OnUpdate();
        }

        // Dispose済みフラグ
        private bool _isDispose = true;

        // Spawner へのプール返却通知済みフラグ
        private bool _isDespawnNotified = false;

        // イベントの登録処理をすでに行っているか
        private bool _isRegisterEvents = false;

        // AnimationのEvent通知者
        private IBossCharacterAnimationEventReceiver _animationEventReceiver = null;

        // Enemyが受けられるサービス群
        private EnemyServices _enemyServices = default;

        // BehaviourTreeのControlクラス
        private BehaviourController _bossAIBehaviourController = null;

        // ビヘイビアツリーの実行状況通知クラス
        private NodeRunningConditionNotifier _nodeRunningConditionNotifier = null;
        
        // CharacterのView
        private IBossEnemyCharacterView _bossCharacterView = null;

        // CharacterのEntity
        private IBossCharacterEntity _characterEntity = null;

        // 複数のストリームを死亡時に同時に止める
        private CompositeDisposable _deadEventDisposables = new CompositeDisposable();

        private void RegisterEvents()
        {
            if (_isRegisterEvents) return;

            // Disposableを初期化
            _deadEventDisposables = new();

            // ビヘイビアツリー探索開始イベント購読開始
            _bossCharacterView.OnBeginsAction += HandleRunningBehaviourTree;

            // 鎧破壊時のイベント購読開始
            _characterEntity.BreakingArmorAttachmentType
                .SkipLatestValueOnSubscribe()
                .Subscribe(breakArmor =>
                {
                    HandleArmorBreak(breakArmor);
                }).AddTo(_deadEventDisposables);

            // 鎧修復時のイベント購読開始
            _characterEntity.RepairArmorAttachmentType
                .SkipLatestValueOnSubscribe()
                .Subscribe(repairArmor =>
                {
                    HandleArmorRepair(repairArmor);
                }).AddTo(_deadEventDisposables);

            // 現在のキャラクターの行動変更時のイベント購読開始
            _characterEntity.CurrentAction
                .SkipLatestValueOnSubscribe()
                .Subscribe(currentAction =>
            {
                switch (currentAction)
                {
                    case CharacterAction.Attacking:
                        HandleExecuteAttack(_characterEntity.ExecutingAttackData);
                        break;
                    case CharacterAction.PhaseChanging:
                        HandlePhaseChange();
                        break;
                    case CharacterAction.PostureChanging:
                        HandleChangePosture(_characterEntity.CurrentCharacterPostureType);
                        break;
                    case CharacterAction.Dead:
                        HandleDead();
                        break;
                }
            }).AddTo(_deadEventDisposables);

            // HPが0になった際のイベント購読開始
            _characterEntity.CurrentHP
                .SkipLatestValueOnSubscribe()
                .Subscribe(currentHP =>
            { if (currentHP == 0) HandleHPZero(); }).AddTo(_deadEventDisposables);

            // 姿勢切り替え完了イベント購読開始
            _animationEventReceiver.OnPostureChangeCompleted += HandlePostureChangeCompleted;

            // 被ダメージイベント購読開始
            _bossCharacterView.OnTakeDamage += HandleTakeDamage;

            // ボスが移動した際のイベント購読開始
            _characterEntity.Position.Subscribe(newPosition => 
            { HandleMovePosition(newPosition); }).AddTo(_deadEventDisposables);

            _characterEntity.Rotation.Subscribe(newRotation => 
            { HandleMoveRotation(newRotation); }).AddTo(_deadEventDisposables);

            _characterEntity.Velocity.Subscribe(newVelocity => 
            { HandleMoveVelocity(newVelocity); }).AddTo(_deadEventDisposables);

            // ボスの攻撃の当たり判定を行うイベント購読開始
            _animationEventReceiver.OnCheckHitAttack += HandleCheckHitAttack;

            // ボスの攻撃が当たった際のイベント購読開始
            _characterEntity.OnAttackHit += HandleAttackHit;

            // 攻撃中止イベント購読開始
            _characterEntity.OnAttackCancel += HandleAttackCancel;

            // ボスが攻撃終了イベント購読開始
            _animationEventReceiver.OnAttackCompleted += HandleAttackCompleted;

            // TimeScale変更時のイベント購読開始
            _bossCharacterView.TimeScaleReactiveProperty
                .Subscribe(timaScale => 
            { HandleChangedTimeScale(timaScale); }).AddTo(_deadEventDisposables);

            // キャラクターの移動イベント購読開始
            _animationEventReceiver.OnMoveCharacter += HandleMoveCharacter;

            // 既にイベントの登録が完了しているフラグを立てる
            _isRegisterEvents = true;
        }

        private void UnregisterEvents()
        {
            if (!_isRegisterEvents) return;

            // ビヘイビアツリー探索開始イベント購読解除
            _bossCharacterView.OnBeginsAction -= HandleRunningBehaviourTree;

            // 攻撃中止イベント購読解除
            _characterEntity.OnAttackCancel -= HandleAttackCancel;

            // 姿勢切り替え完了イベント購読解除
            _animationEventReceiver.OnPostureChangeCompleted -= HandlePostureChangeCompleted;

            // 被ダメージイベント購読解除
            _bossCharacterView.OnTakeDamage -= HandleTakeDamage;

            // ボスの攻撃の当たり判定を行うイベント購読解除
            _animationEventReceiver.OnCheckHitAttack -= HandleCheckHitAttack;

            // ボスの攻撃が当たった際のイベント購読解除
            _characterEntity.OnAttackHit -= HandleAttackHit;

            // ボスが攻撃を終了したことの通知をアニメーター側から受け取るイベント購読解除
            _animationEventReceiver.OnAttackCompleted -= HandleAttackCompleted;

            // キャラクターの移動イベント購読解除
            _animationEventReceiver.OnMoveCharacter -= HandleMoveCharacter;

            // Subscribe解除
            _deadEventDisposables.Dispose();

            // 既にイベントの登録が完了しているフラグをFalseに
            _isRegisterEvents = false;
        }

        /// <summary> BehaviourTreeの探索を開始する </summary>
        private void HandleRunningBehaviourTree()
        {
            Debug.Log("ビヘイビアツリー探索");
            _nodeRunningConditionNotifier.HandleResearchBehaviourTree();
        }

        /// <summary> 死亡イベント発火時の処理 </summary>
        private void HandleDead()
        {
            if (_isDispose) return;

            if (_characterEntity.ExecutingAttackData.ID != 0)
                _characterEntity.CancelAttack();

            _bossAIBehaviourController.StopRunning();
            _bossAIBehaviourController.Dispose();
            UnregisterEvents();

            _bossCharacterView.HandleDead();

            _isDispose = true;
        }

        /// <summary> 鎧破壊イベント発火時の処理 </summary>
        private void HandleArmorBreak(ArmorAttachmentType armorAttachmentType)
        {
            if (armorAttachmentType == ArmorAttachmentType.None) return;

            _bossCharacterView.BreakArmor(armorAttachmentType);

            HandleRunningBehaviourTree();

            _characterEntity.ResetArmorBreakingEvent();
        }

        /// <summary> 鎧修復イベント発火時の処理 </summary>
        private void HandleArmorRepair(ArmorAttachmentType armorAttachmentType)
        {
            if (armorAttachmentType == ArmorAttachmentType.None) return;

            _bossCharacterView.RepairArmor(armorAttachmentType);

            _characterEntity.ResetRepairArmorEvent();
        }

        /// <summary> ボスのHPが0になった際のイベント発火時の処理 </summary>
        private void HandleHPZero()
        {
            HandleRunningBehaviourTree();
        }

        /// <summary> フェーズ切り替えイベント発火時の処理 </summary>
        private void HandlePhaseChange()
        {
            // もし攻撃実行中であったのであれば攻撃を中断する
            if(_characterEntity.ExecutingAttackData.ID != 0)
            {
                _characterEntity.CancelAttack();
            }

            _bossCharacterView.ChangePhase(_characterEntity.CharacterCurrentStats.PhaseNum);
        }

        /// <summary> ボスの体勢が変わった際のイベント発火時の処理 </summary>
        /// <param name="posture"> ボスの体勢 </param>
        private void HandleChangePosture(PostureType posture)
        {
            // ダウンへ遷移する場合、進行中の攻撃処理を残さない。
            if(_characterEntity.ExecutingAttackData.ID != 0)
            {
                if (posture == PostureType.LeftHalfKneel
                || posture == PostureType.RightHalfKneel
                || posture == PostureType.SpreadEagled)
                {
                    _characterEntity.CancelAttack();
                }
            }

            _bossCharacterView.ChangePosture(posture);
        }

        /// <summary> ボスの体勢変化が完了した際のイベント発火時の処理 </summary>
        private void HandlePostureChangeCompleted() 
        {
            // 現在の行動を元に戻す
            _characterEntity.SetCurrentAction(CharacterAction.Idle);
        }

        /// <summary> 被ダメージイベント発火時の処理 </summary>
        /// <param name="damageContext"> 被ダメージ状況 </param>
        /// <param name="hitPartsType"> 攻撃被弾ヶ所 </param>
        /// <param name="scapegoatArmor"> 被弾ヶ所が鎧装着時に身代わりとなる鎧の部位 </param>
        private void HandleTakeDamage(DamageContext damageContext, TakeDamageType hitPartsType, ArmorAttachmentType scapegoatArmor)
        {
            Logic.Damage.TakeDamage(_characterEntity, damageContext, hitPartsType, scapegoatArmor);
        }

        /// <summary> 移動イベント発火時の処理 </summary>
        private void HandleMovePosition(Vector3 position)
        {
            _bossCharacterView.SetPosition(position);
        }

        /// <summary> Characterに回転が加わった際のイベント発火時の処理 </summary>
        private void HandleMoveRotation(Quaternion quaternion)
        {
            _bossCharacterView.SetRotation(quaternion);
        }

        /// <summary> 移動によってVelocityの値が変わったときの処理 </summary>
        private void HandleMoveVelocity(Vector3 velocity)
        {
            _bossCharacterView.SetVelocity(velocity);
        }

        /// <summary> ボスの攻撃開始イベント発火時の処理 </summary>
        private void HandleExecuteAttack(Attack.AttackData executingAttackData)
        {
            _bossCharacterView.ExecuteAttack(executingAttackData);
        }

        /// <summary> ボスの攻撃が終了した際のイベント発火時の処理 </summary>
        private void HandleAttackCancel()
        {
            _bossCharacterView.StopActiveAttacks();
        }

        /// <summary> ボスの攻撃が終了した際のイベント発火時の処理 </summary>
        private void HandleAttackCompleted()
        {
            _characterEntity.AttackCompleted();
        }

        /// <summary> ボスの攻撃がターゲットに当たった際のイベント発火時の処理 </summary>
        private void HandleCheckHitAttack(Attack.AttackData tryHitAttackData, AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward)
        {
            _characterEntity.TryHitAttackDamageToTarget(tryHitAttackData, attackHitAreaType, attackPosition, forward);
        }

        /// <summary> 攻撃が当たった際のイベント </summary>
        private void HandleAttackHit()
        {
            _animationEventReceiver.AnimEvent_HitAttack();
        }

        /// <summary> TimeScale変更イベント発火時の処理 </summary>
        private void HandleChangedTimeScale(float timeScale)
        {
            _characterEntity.SetTimeScale(timeScale);
        }

        /// <summary> キャラクター移動イベント発火時の処理 </summary>
        private void HandleMoveCharacter(Vector3 goalPos, float moveTime)
        {
            Logic.Movement.MoveTargetPositionRightOnTime
                (_characterEntity, goalPos, moveTime, _characterEntity.TimeScale);
        }

    }
}
