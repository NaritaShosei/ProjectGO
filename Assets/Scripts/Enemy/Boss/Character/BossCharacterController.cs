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
        public void Init(IBossEnemyCharacterView bossEnemyCharacterView,
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
        }

        public void OnUpdate()
        {
            if(_bossAIBehaviourController != null)
                _bossAIBehaviourController.OnUpdate();
        }

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
            // Disposableを初期化
            _deadEventDisposables = new();

            // ビヘイビアツリー探索開始イベント
            _bossCharacterView.OnBeginsAction += HandleRunningBehaviourTree;
            _characterEntity.OnArmorBreak += HandleRunningBehaviourTree;

            // Phase切り替えイベント登録
            _characterEntity.IsPhaseChaging.Subscribe(isPhaseChanging =>
            {
                if (isPhaseChanging) HandlePhaseChange();
            }).AddTo(_deadEventDisposables);

            // Phase切り替え完了時イベント登録
            _animationEventReceiver.OnPhaseChangeEnd += HandlePhaseChangeEnd;

            // 死亡時のイベント登録
            _characterEntity.OnDead += HandleDead;

            // 姿勢切り替えイベント登録
            _characterEntity.CurrentCharacterPostureType.Subscribe(posture =>
            {
                HandleChangePosture(posture);
            }).AddTo(_deadEventDisposables);

            // 被ダメージイベント登録
            _bossCharacterView.OnTakeDamage += HandleTakeDamage;

            // ボスが移動した際のイベント登録
            _characterEntity.Position.Subscribe(newPosition => { HandleMovePosition(newPosition); }).AddTo(_deadEventDisposables);
            _characterEntity.Rotation.Subscribe(newRotation => { HandleMoveRotation(newRotation); }).AddTo(_deadEventDisposables);
            _characterEntity.Velocity.Subscribe(newVelocity => { HandleMoveVelocity(newVelocity); }).AddTo(_deadEventDisposables);

            // ボスが攻撃を行った際のイベント登録
            _characterEntity.ExecutingAttackData.Subscribe(attack =>
            {
                if(attack.ID != 0) HandleExecuteAttack(attack);
            }).AddTo(_deadEventDisposables);

            // ボスの攻撃の当たり判定を行うイベント
            _animationEventReceiver.OnCheckHitAttack += HandleCheckHitAttack;

            // ボスが攻撃を終了したことの通知をアニメーター側から受け取る
            _animationEventReceiver.OnAttackEnd += HandleAttackEnd;

            // TimeScale変更時のイベント
            _bossCharacterView.OnChangedTimeScale += HandleChangedTimeScale;
        }

        private void UnregisterEvents()
        {
            _bossCharacterView.OnBeginsAction -= HandleRunningBehaviourTree;
            _characterEntity.OnArmorBreak -= HandleRunningBehaviourTree;
            _bossCharacterView.OnTakeDamage -= HandleTakeDamage;
            _characterEntity.OnDead -= HandleDead;
            _animationEventReceiver.OnCheckHitAttack -= HandleCheckHitAttack;
            _animationEventReceiver.OnAttackEnd -= HandleAttackEnd;
            _bossCharacterView.OnChangedTimeScale -= HandleChangedTimeScale;

            _deadEventDisposables.Dispose();
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
            UnregisterEvents();
        }

        /// <summary> フェーズ切り替えイベント発火時の処理 </summary>
        private void HandlePhaseChange()
        {
            _bossCharacterView.ChangePhase(_characterEntity.CharacterCurrentStats.PhaseNum);
        }

        private void HandlePhaseChangeEnd()
        {
            _characterEntity.PhaseChangeEnd();
        }

        /// <summary> ボスの体勢が変わった際のイベント発火時の処理 </summary>
        /// <param name="posture"> ボスの体勢 </param>
        private void HandleChangePosture(PostureType posture)
        {
            _bossCharacterView.ChangePosture(posture);
        }

        /// <summary> 被ダメージイベント発火時の処理 </summary>
        /// <param name="damageContext"> 被ダメージ状況 </param>
        /// <param name="hitPartsType"> 攻撃被弾ヶ所 </param>
        /// <param name="scapegoatArmor"> 被弾ヶ所が鎧装着時に身代わりとなる鎧の部位 </param
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
        private void HandleAttackEnd()
        {
            _characterEntity.AttackEnd();

            HandleRunningBehaviourTree();
        }

        /// <summary> ボスの攻撃がターゲットに当たった際のイベント発火時の処理 </summary>
        private void HandleCheckHitAttack(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward)
        {
            _characterEntity.TryHitAttackDamageToTarget(attackHitAreaType, attackPosition, forward);
        }

        /// <summary> TimeScale変更イベント発火時の処理 </summary>
        private void HandleChangedTimeScale(float timeScale)
        {
            _characterEntity.SetTimeScale(timeScale);
        }
    }
}
