using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;

#region BossEnemy関連
using BossEnemy.Model.System;
using BossEnemy.Model.Interface;
using BossEnemy.View.SMB;
using BossEnemy.Application;
using BossEnemy.Enum;
using BossEnemy.Data;
using System.Collections.Generic;
#endregion

namespace BossEnemy.View
{
    /// <summary>
    /// ボス本体のViewClass
    /// </summary>
    public class BossEnemyView : MonoBehaviour, IEnemy, IPoolable, ISpeedChange
    {
        // --- Events ---

        /// <summary>HP変化時に発火するイベント（current, max）</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>ダメージを受けた際にポップアップ情報を通知するイベント</summary>
        public event Action<DamagePopupViewModel> OnDamageDealt;

        /// <summary>ダメージを受けて生存したときに発火するイベント（被弾入れ替え判定に使用）</summary>
        public event Action<IEnemy> OnDamaged;

        /// <summary>ダメージを受けたときに発火するイベント</summary>
        public event Action<DamageContext, BodysDefensesType, bool, ArmorAttachmentPointType> OnTakeDamage;

        /// <summary>死亡時に発火するイベント</summary>
        public event Action<IEnemy> OnDead;

        /// <summary>ロックオン可能なパーツが変わった際ベント<新しいターゲット、古いターゲット></summary>
        public event Action<IReadOnlyList<ILockOnTarget>, IReadOnlyList<ILockOnTarget>> OnChangeLockOnParts;

        // --- Properties ---
        /// <summary> BossEnemyと内部Modelを繋ぐControllerClass </summary>
        public BossEnemyController BossEnemyController => _bossEnemyController;

        /// <summary>ConditionController への参照</summary>
        public IEnemyConditionController ConditionController { get; }

        /// <summary>EnemyAnimator への参照</summary>
        public IEnemyAnimator EnemyAnimator => null;

        /// <summary> 自身のTransformへの参照 </summary>
        public Transform Self => transform;

        /// <summary>インスタンス識別ID（AttackerSlotのキーに使用）</summary>
        public int Id { get; }

        /// <summary>ボス判定</summary>
        public bool IsBoss => true;

        /// <summary>HitStop等で使用するタイムスケール（DeadCondition の物理スケーリングに使用）</summary>
        public float TimeScale
        {
            get { return _timeScale.Value; }
            set { _timeScale.Value = value; }
        }

        public IReadOnlyReactiveProperty<float> TimeScaleProperty => _timeScale;

        /// <summary> 死亡判定 </summary>
        public bool IsDead => _isDead;

        /// <summary> ロックオン可能か(非アクティブ状態でオフにしたい場合など)。 </summary>
        public bool IsLockable => _isLockable;

        /// <summary> 現在攻撃可能なボスの部位 </summary>
        public BossEnemyPartsView[] ActiveBossEnemyPartsView => _activeBossEnemyPartsView;

        // --- Methods ---

        /// <summary> 初期化する </summary>
        public void Init()
        {
            _isDead = false;
            _isLockable = true;
            SetPosture(PostureType.Stand);

            if(!ServiceLocator.TryGet(out _cameraManager))
            {
                Debug.Log("取得失敗");
            }

            foreach (var parts in _activeBossEnemyPartsView)
            {
                parts.Init(this);
            }

            foreach (var behaviour in _animator.GetBehaviours<AttackSMBBase>())
            {
                behaviour.Init(_bossEnemyAnimationEventReceiver, _bossEnemyAnimator, _attackInformationHolder,
                    _cameraManager, _attackHitAreaSpawner, Self, _services.PlayerInformationService.Player);
            }

            _bossEnemyController.Init(_services, _bossEnemyAnimationEventReceiver);
        }

        /// <summary>攻撃の内容を渡して内部でダメージ計算をする</summary>
        public void TakeDamage(DamageContext context)
        {
            DamagePopupViewModel damagePopupViewModel;
            BossEnemyPartsView parts = null;

            // 攻撃から一番近いボスエネミーのパーツを割り出す
            float saveClosestDistance = 1000;
            bool isGuardArmor = false;
            ArmorAttachmentPointType armorAttachmentPoint = ArmorAttachmentPointType.None;
            Vector3 hitPos = Vector3.zero;
            bool isWeekPoint = false;
            bool isHitArmor = false;
            foreach (var bossParts in _activeBossEnemyPartsView)
            {
                float playerDistance = _services.PlayerInformationService.ToPlayerDistance(bossParts.PartsPosition);

                if (playerDistance < saveClosestDistance)
                {
                    saveClosestDistance = playerDistance;
                    parts = bossParts;

                    isGuardArmor = false;
                    armorAttachmentPoint = ArmorAttachmentPointType.None;

                    if (bossParts.Armor != null && !bossParts.Armor.IsBreak)
                    {
                        isHitArmor = true;
                        isGuardArmor = true;
                        armorAttachmentPoint = bossParts.Armor.AttachmentPoints;
                    }

                    hitPos = bossParts.PartsPosition; 

                    switch (bossParts.PartsType)
                    {
                        case BodysDefensesType.None:
                            break;
                        case BodysDefensesType.Normal:
                            isWeekPoint = false;
                            break;
                        case BodysDefensesType.Hard:
                            isWeekPoint = false;
                            break;
                        case BodysDefensesType.WeekPoint:
                            isWeekPoint = true;
                            break;
                        case BodysDefensesType.VitalPoint:
                            isWeekPoint = true;
                            break;
                    }
                }
            }

            // ダメージのポップアップ
            HitResult result = new HitResult()
            {
                IsKill = false,
                IsArmorBreak = false,
                IsWeakPoint = isWeekPoint,
                IsArmorHit = isHitArmor,
            };
            context.OnHitResult?.Invoke(result);

            damagePopupViewModel = new(DamageSystem.CalculateDamage(context, default), isWeekPoint, true, hitPos);
            OnDamageDealt?.Invoke(damagePopupViewModel);

            Debug.Log("ダメージを検出(アーマーのガード:" + isGuardArmor + ")" + "(こうげきかしょ:" + parts.PartsType + ")");
            OnTakeDamage?.Invoke(context, parts.PartsType, isGuardArmor, armorAttachmentPoint);

            // 攻撃を受けた際のEffect
            if (isHitArmor) _effectManager.PlayEffect(_takeArmorDamageEffectKey, hitPos);
            else _effectManager.PlayEffect(_takeDamageEffectKey, hitPos);
        }

        public void Attack(BossEnemyAttackData bossEnemyAttackData)
        {
            _attackInformationHolder.SetData(bossEnemyAttackData);
            _bossEnemyAnimator.SetAttacking(true, bossEnemyAttackData.AnimParamName);
            Debug.Log(bossEnemyAttackData.AnimParamName);
        }

        public void AttackEnd()
        {
            _bossEnemyAnimator.SetAttacking(false);
        }

        public void Down(bool isBreakLeftLeg, bool isBreakrightLeg)
        {
            _bossEnemyAnimator.SetBreakingArmor(isBreakLeftLeg, isBreakrightLeg);

            if (isBreakLeftLeg && isBreakrightLeg)
            {
                PlayBossSE(SoundCueNames.Boss.TwoLegBreakDownVoice);
                PlayBossSE(SoundCueNames.Boss.TwoLegBreakDownImpact);
                SetPosture(PostureType.SpreadEagled);
                return;
            }

            if (isBreakLeftLeg || isBreakrightLeg)
            {
                PlayBossSE(SoundCueNames.Boss.OneLegBreakVoice);
                PlayBossSE(SoundCueNames.Boss.OneLegBreakDownImpact);
                SetPosture(PostureType.Crouch);
            }
        }

        public void RiseUp()
        {
            _bossEnemyAnimator.SetIsDown(false);
            SetPosture(PostureType.Stand);
        }

        #region 鎧関連の処理
        public void ArmorInit()
        {
            foreach (var bossArmor in _bossArmorViews)
            {
                bossArmor.Init();
            }
        }

        public void ArmorBreak(ArmorAttachmentPointType attachmentPointsType)
        {
            foreach (var bossArmor in _bossArmorViews)
            {
                if (bossArmor.AttachmentPoints == attachmentPointsType)
                {
                    bossArmor.BreakArmer().Forget();
                }
            }
        }

        public void ArmorRepair(ArmorAttachmentPointType attachmentPointsType)
        {
            foreach (var bossArmor in _bossArmorViews)
            {
                if (bossArmor.AttachmentPoints == attachmentPointsType)
                {
                    bossArmor.RepairArmor().Forget();
                }
            }
        }
        #endregion

        /// <summary>ノックバックの力を与える</summary>
        public void AddKnockbackForce(Vector3 direction)
        {

        }

        /// <summary>ConditionによりActionを阻害する</summary>
        public void OnConditionInterrupt()
        {

        }

        public void PhaseChange()
        {
            _bossEnemyAnimator.SetPhaseChange();
        }

        public void SetPosture(PostureType postureType)
        {
            if (postureType == _currentPostureType) return;

            BossEnemyPartsView[] lockOnOldTargets = null;
            if (_activeBossEnemyPartsView != null)
            {
                lockOnOldTargets = _activeBossEnemyPartsView;
                foreach (var oldTarget in _activeBossEnemyPartsView)
                {
                    oldTarget.SetLockable(false);
                }
            }

            foreach (var collision in _collisionDetections)
            {
                if (collision.CollisionDetectionPostureType == postureType)
                {
                    _bossCollider.size = collision.BossColliderSize;
                    _bossCollider.center = collision.BossColliderCenter;
                    _activeBossEnemyPartsView = collision.BossEnemyPartsView;
                    _currentPostureType = postureType;

                    BossEnemyPartsView[] lockOnNewTargets = _activeBossEnemyPartsView;

                    foreach(var newTarget in _activeBossEnemyPartsView)
                    {
                        newTarget.SetLockable(true);
                    }

                    OnChangeLockOnParts.Invoke(lockOnNewTargets, lockOnOldTargets);
                    return;
                }
            }
        }

        /// <summary>ColliderのIsTriggerをセットする</summary>
        public void SetIsTrigger(bool isTrigger)
        {
            _bossCollider.isTrigger = isTrigger;
        }

        /// <summary>位置をセットする</summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>回転をセットする</summary>
        public void SetRotation(Quaternion quaternion)
        {
            transform.rotation = quaternion;
        }

        /// <summary>速度をセットする</summary>
        public void SetVelocity(Vector3 velocity)
        {
            _bossEnemyAnimator.SetSpeed(velocity.x, velocity.z);
        }

        /// <summary> 各種Spawnerを設定する </summary>
        public void SetSpawner(IAttackHitAreaSpawner attackHitAreaSpawner)
        {
            _attackHitAreaSpawner = attackHitAreaSpawner;
        }

        /// <summary>各サービスを注入する。EnemyManagerのSpawnから呼ぶ想定</summary>
        public void InjectServices(EnemyServices services)
        {
            _services = services;
        }

        /// <summary>
        /// ロックオンなどの中心のTransformを取得する
        /// </summary>
        public Transform GetTargetCenter()
        {
            return _targetCenterTransform;
        }

        public void OnSpeedChange(float timeScale)
        {
            _timeScale.Value = timeScale;
            _bossEnemyAnimator.SetAnimSpeed(timeScale);
        }

        /// <summary>
        /// プールから取り出された直後に呼ばれる。
        /// 状態のリセットや初期化処理を実装する。
        /// </summary>
        public void OnGet()
        {

        }

        /// <summary>
        /// プールへ返却される直前に呼ばれる。
        /// 後始末（イベント解除・Tween停止など）を実装する。
        /// </summary>
        public void OnRelease()
        {
            _isLockable = false;
        }

        /// <summary> 死んだ際の処理 </summary>
        public void Dead()
        {
            if (_isDead) return;

            _isDead = true;
            _bossEnemyAnimator.SetDead();
            OnDead?.Invoke(this);
            Debug.Log("Boss討伐完了");
        }

        [Header("BossEnemyのController")]
        [SerializeField, Tooltip("BossEnemyのViewとModelの仲介役")]
        private BossEnemyController _bossEnemyController;

        [Header("ボスの当たり判定")]
        [SerializeField] private CollisionInformation[] _collisionDetections;

        [Header("ボスエネミーの各所鎧のView")]
        [SerializeField] private BossArmorView[] _bossArmorViews;

        [Header("ロックオン対象のTransform")]
        [SerializeField] private Transform _targetCenterTransform;

        [Header("ボスエネミーのAnimator")]
        [SerializeField] private Animator _animator;

        [Header("ボスエネミーの当たり判定")]
        [SerializeField] private BoxCollider _bossCollider;

        [Header("ボスエネミーのAnimationEventReceiver")]
        [SerializeReference, SubclassSelector]
        private IAnimationEventReceiver _bossEnemyAnimationEventReceiver;

        private BossEnemyAnimator _bossEnemyAnimator;
        private AttackInformationHolder _attackInformationHolder = new();
        private EffectManager _effectManager;
        private CameraManager _cameraManager;

        private EnemyServices _services;

        // 各種Spawner
        private IAttackHitAreaSpawner _attackHitAreaSpawner = null;

        private bool _isDead = false;
        private bool _isLockable;

        private ReactiveProperty<float> _timeScale = new(1.0f);

        private BossEnemyPartsView[] _activeBossEnemyPartsView;
        private PostureType _currentPostureType = PostureType.None;

        private const string _takeArmorDamageEffectKey = "BossArmorHit";
        private const string _takeDamageEffectKey = "テスト血しぶき";

        private void Awake()
        {
            _bossEnemyAnimator = new BossEnemyAnimator(_animator, _bossEnemyAnimationEventReceiver);
            _effectManager = FindFirstObjectByType<EffectManager>();
        }

        private void Update()
        {
            if (_bossEnemyController == null) return;

            if (!_isDead) _bossEnemyController.OnUpdate();
        }

        private void PlayBossSE(string cueName)
        {
            Sound.PlaySE(gameObject, cueName, CueSheetType.Boss);
        }

        #region ボスの姿勢ごとの当たり判定情報
        [Serializable]
        public struct CollisionInformation
        {
            public PostureType CollisionDetectionPostureType => _postureType;

            public Vector3 BossColliderSize => _size;

            public Vector3 BossColliderCenter => _center;

            public BossEnemyPartsView[] BossEnemyPartsView => _bossEnemyPartsView;

            [Header("姿勢")]
            [SerializeField] private PostureType _postureType;

            [Header("当たり判定のCollider情報")]
            [SerializeField] private Vector3 _center;
            [SerializeField] private Vector3 _size;

            [Header("各部位")]
            [SerializeField] private BossEnemyPartsView[] _bossEnemyPartsView;
        }
        #endregion

        #region ボスエネミーの各部位
        [Serializable]
        public class BossEnemyPartsView : ILockOnTarget
        {
            /// <summary>
            /// ロックオンなどの中心のTransformを取得する
            /// </summary>
            public Transform GetTargetCenter()
            {
                return _partsTransform;
            }

            /// <summary>
            /// ロックオン可能か(非アクティブ状態でオフにしたい場合など)。
            /// </summary>
            public bool IsLockable => _isLockable;

            /// <summary>
            /// パーツの座標
            /// </summary>
            public Vector3 PartsPosition => _partsTransform.position;

            /// <summary>
            /// このパーツにつけるアーマー
            /// </summary>
            public BossArmorView Armor => _thisPartsArmer;

            /// <summary>
            /// このパーツの硬さ(肉質)
            /// </summary>
            public BodysDefensesType PartsType => _bossEnemyPartsType;

            public void Init(BossEnemyView bossEnemyView)
            {
                _bossEnemyView = bossEnemyView;

                if (_thisPartsArmer != null) _thisPartsArmer.Init();
            }

            public void SetLockable (bool lockable) => _isLockable = lockable;

            [Header("このPartsのTransform")]
            [SerializeField] private Transform _partsTransform;

            [Header("このパーツに装備するアーマー(なければNullにする)")]
            [SerializeField] private BossArmorView _thisPartsArmer = null;

            [Header("このPartsの硬さ(肉質)")]
            [SerializeField] private BodysDefensesType _bossEnemyPartsType;

            private bool _isLockable = false;

            private BossEnemyView _bossEnemyView = null;
        }
        #endregion
    }

}
