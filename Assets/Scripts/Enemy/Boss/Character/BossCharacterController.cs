using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Interface;
using BossEnemy.AI.BehaviourTree;
using BossEnemy.Attack;

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
            _characterEntity = bossCharacterEntity;

            RegisterEvents();
        }

        public void OnUpdate()
        {
            if(_bossAIBehaviourController != null)
                _bossAIBehaviourController.OnUpdate();
        }

        /// <summary> BehaviourTreeの探索を開始する </summary>
        public void HandleRunningBehaviourTree()
        {
            Debug.Log("探索開始");
            _bossAIBehaviourController.SearchNextRunningNode();
        }

        /// <summary> 死亡イベント発火時の処理 </summary>
        public void HandleDead()
        {
            UnregisterEvents();
        }

        /// <summary> フェーズ切り替えイベント発火時の処理 </summary>
        public void HandlePhaseChange()
        {

        }

        /// <summary> ボスの体勢が変わった際のイベント発火時の処理 </summary>
        /// <param name="posture"> ボスの体勢 </param>
        public void HandleChangePosture(PostureType posture)
        {

        }

        /// <summary> 被ダメージイベント発火時の処理 </summary>
        /// <param name="damageContext"> 被ダメージ状況 </param>
        /// <param name="hitPartsType"> 攻撃被弾ヶ所 </param>
        /// <param name="scapegoatArmor"> 被弾ヶ所が鎧装着時に身代わりとなる鎧の部位 </param
        public void HandleTakeDamage(DamageContext damageContext, TakeDamageType hitPartsType, ArmorAttachmentType scapegoatArmor)
        {

        }

        /// <summary> 移動イベント発火時の処理 </summary>
        public void HandleMovePosition(Vector3 position)
        {

        }

        /// <summary> Characterに回転が加わった際のイベント発火時の処理 </summary>
        public void HandleChangeRotation(Quaternion quaternion)
        {

        }

        /// <summary> 移動によってVelocityの値が変わったときの処理 </summary>
        public void HandleChangeMoveVelocity(Vector3 velocity)
        {

        }

        /// <summary> ボスの攻撃開始イベント発火時の処理 </summary>
        public void HandleAttackStart()
        {
            
        }

        /// <summary> ボスの攻撃が終了した際のイベント発火時の処理 </summary>
        public void HandleAttackEnd()
        {
            
        }

        /// <summary> ボスの攻撃がターゲットに当たった際のイベント発火時の処理 </summary>
        public void HandleCheckHitAttack()
        {
            //_characterEntity.CheckHitAttack();
        }

        /// <summary> TimeScale変更イベント発火時の処理 </summary>
        public void HandleChangedTimeScale(float timeScale)
        {
            _characterEntity.SetTimeScale(timeScale);
        }

        // AnimationのEvent通知者
        private IBossCharacterAnimationEventReceiver _animationEventReceiver = null;

        // Enemyが受けられるサービス群
        private EnemyServices _enemyServices = default;

        // BehaviourTreeのControlクラス
        private BehaviourController _bossAIBehaviourController = null;
        
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
            _characterEntity.Rotation.Subscribe(newRotation => { HandleChangeRotation(newRotation); }).AddTo(_deadEventDisposables);
            _characterEntity.Velocity.Subscribe(newVelocity => { HandleChangeMoveVelocity(newVelocity); }).AddTo(_deadEventDisposables);

            // ボスが攻撃を行った際のイベント登録
            _characterEntity.IsAttacking.Subscribe(isAttacking =>
            {
                if (isAttacking) HandleAttackStart();
            }).AddTo(_deadEventDisposables);

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

            _deadEventDisposables.Dispose();
        }
    }
}
