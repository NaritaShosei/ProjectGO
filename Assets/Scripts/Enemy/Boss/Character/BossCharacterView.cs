using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;
using System.Collections.Generic;
using BossEnemy.Armor;
using BossEnemy.Animation;
using BossEnemy.Interface;
using BossEnemy.Enum;
using BossEnemy.SMB;

namespace BossEnemy.Character
{
    /// <summary>
    /// ボス本体のViewClass
    /// </summary>
    public class BossCharacterView : MonoBehaviour, IBossEnemyCharacterView
    {
        private const string TAKE_DAMAGE_ARMOR_EFFECT_KEY = "BossArmorHit";
        private const string TAKE_DAMAGE_EFFECT_KEY = "テスト血しぶき";

        // --- Events ---

        /// <summary>HP変化時に発火するイベント（current, max）</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>ダメージを受けた際にポップアップ情報を通知するイベント</summary>
        public event Action<DamagePopupViewModel> OnDamageDealt;

        /// <summary>ダメージを受けて生存したときに発火するイベント（被弾入れ替え判定に使用）</summary>
        public event Action<IEnemy> OnDamaged;

        /// <summary> 姿勢変更後発火されるイベント </summary>
        public event Action<PostureType> OnChangedPosture;


        /// <summary>ダメージを受けたときに発火するイベント</summary>
        public event Action<DamageContext, TakeDamageType, ArmorAttachmentType> OnTakeDamage;

        /// <summary>死亡時に発火するイベント</summary>
        public event Action<IEnemy> OnDead;

        /// <summary>ロックオン可能なパーツが変わった際のイベント<新しいターゲット、古いターゲット></summary>
        public event Action<(IReadOnlyList<ILockOnTarget> newTargetParts, IReadOnlyList<ILockOnTarget> oldTargetParts)> OnChangeLockOnParts;

        /// <summary> Bossのすべての初期化が終了して動き出す際のイベント </summary>
        public event Action OnBeginsAction;

        // --- Properties ---
        /// <summary> BossEnemyと内部Modelを繋ぐControllerClass </summary>
        public IBossEnemyCharacterController BossEnemyController => _bossEnemyController;

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
        public float TimeScale => _timeScale.Value;

        /// <summary> タイムスケールの数値変更時に発火するReactiveProperty </summary>
        public IReadOnlyReactiveProperty<float> TimeScaleReactiveProperty => _timeScale;

        /// <summary> 死亡判定 </summary>
        public bool IsDead => _isDead;

        /// <summary> ロックオン可能か(非アクティブ状態でオフにしたい場合など)。 </summary>
        public bool IsLockable => _isLockable;

        /// <summary> 現在攻撃可能なボスの部位 </summary>
        public BossCharacterPartsView[] ActiveBossEnemyPartsView => _activeCollisionPartsView;

        /// <summary> アニメーション駆動によるイベント受け取りクラス </summary>
        public IBossCharacterAnimationEventReceiver BossEnemyAnimationEventReceiver => _bossEnemyAnimationEventReceiver;

        // --- Methods ---

        /// <summary> 初期化する </summary>
        public void Init()
        {
            _isDead = false;
            _isLockable = true;
            _attackSMBList = new();

            // 鎧の初期化
            InitArmor();

            if (!ServiceLocator.TryGet(out _cameraManager))
            {
                Debug.Log("取得失敗");
                return;
            }

            if(!ServiceLocator.TryGet(out _effectManager))
            {
                Debug.Log("取得失敗");
                return;
            }

            foreach (var collisionDetection in _collisionDetections)
            {
                foreach(var parts in collisionDetection.BossEnemyPartsView)
                {
                    parts.Init(this);
                }
            }

            ChangeLockOnParts(PostureType.Standing);

            foreach (var bossCharacterSMB in _animator.GetBehaviours<BossCharacterSMB>())
            {
                if(bossCharacterSMB is AttackSMB attackSMB)
                {
                    attackSMB.Init(
                        _bossEnemyAnimationEventReceiver,
                        this,
                        _attackHitAreaSpawner,
                        _effectManager,
                        _cameraManager,
                        GetTargetCenter(),
                        _services.PlayerInformationService.Player);

                    _attackSMBList.Add(attackSMB);

                    continue;
                }

                bossCharacterSMB.Init(_bossEnemyAnimationEventReceiver, this, GetTargetCenter());
            }
        }

        public void Init(IBossEnemyCharacterController bossEnemyCharacterController)
        {
            Init();

            _bossEnemyController = bossEnemyCharacterController;
        }

        public void StartAction()
        {
            OnBeginsAction?.Invoke();
        }

        /// <summary>攻撃の内容を渡して内部でダメージ計算をする</summary>
        public void TakeDamage(DamageContext context)
        {
            BossCharacterPartsView hitParts = null;

            // 攻撃から一番近いボスエネミーのパーツを割り出す
            float saveClosestDistance = 1000;
            ArmorAttachmentType armorAttachmentPoint = ArmorAttachmentType.None;
            Vector3 hitPos = Vector3.zero;
            bool isWeekPoint = false;
            bool isHitArmor = false;

            if (_activeCollisionPartsView == null)
            {
                Debug.LogError("現在の姿勢が設定されていない可能性があります");
                return;
            }

            // 攻撃の当たった部位を取得
            foreach (var bossParts in _activeCollisionPartsView)
            {
                float playerDistance = _services.PlayerInformationService.ToPlayerDistance(bossParts.PartsPosition);

                if (playerDistance < saveClosestDistance)
                {
                    saveClosestDistance = playerDistance;
                    hitParts = bossParts;
                }
            }

            if (hitParts.Armor != null && !hitParts.Armor.IsBroken)
            {
                isHitArmor = true;
                armorAttachmentPoint = hitParts.Armor.AttachmentPoints;
            }

            hitPos = hitParts.PartsPosition;
            isWeekPoint = IsHitPartsWeekPoint(hitParts);

            // 攻撃Hitイベントの発火
            HitResult result = new HitResult()
            {
                IsKill = false,
                IsArmorBreak = false,
                IsWeakPoint = isWeekPoint,
                IsArmorHit = isHitArmor,
            };
            context.OnHitResult?.Invoke(result);

            // ダメージのポップアップ
            DamagePopUp(context, hitParts, isWeekPoint);

            // ダメージを受けた際のイベント発火
            HandleTakeDamage(context, hitParts, armorAttachmentPoint);

            // ダメージエフェクトの再生
            PlayDamageHitEffect(isHitArmor, hitPos);
        }

        public void ExecuteAttack(Attack.AttackData bossEnemyAttackData)
        {
            foreach (var attack in _attackSMBList)
            {
                if(attack.AttackID == bossEnemyAttackData.ID)
                {
                    attack.SetAttackData(bossEnemyAttackData);
                }
            }

            _bossEnemyAnimator.SetAttacking(true, bossEnemyAttackData.ID);
        }

        public void AttackCompleted()
        {
            _bossEnemyAnimator.SetAttacking(false, 0);
        }

        public void FinishAttackAnimation()
        {
            _bossEnemyAnimator.SetAttacking(false, 0);
        }

        /// <summary>ノックバックの力を与える</summary>
        public void AddKnockbackForce(Vector3 direction)
        {

        }

        /// <summary>ConditionによりActionを阻害する</summary>
        public void OnConditionInterrupt()
        {

        }

        public void ChangePhase(int nextPhase)
        {
            RepairArmor();

            _bossEnemyAnimator.SetPhaseChange(nextPhase);
        }

        /// <summary> キャラクターの姿勢を変更 </summary>
        public void ChangePosture(PostureType postureType)
        {
            if(postureType == PostureType.SpreadEagled)
            {
                PlayBossSE(SoundCueNames.Boss.TwoLegBreakDownVoice);
                PlayBossSE(SoundCueNames.Boss.TwoLegBreakDownImpact);
            }

            if (postureType == PostureType.RightHalfKneel || postureType == PostureType.LeftHalfKneel)
            {
                PlayBossSE(SoundCueNames.Boss.OneLegBreakVoice);
                PlayBossSE(SoundCueNames.Boss.OneLegBreakDownImpact);
            }

            _bossEnemyAnimator.SetPosture(postureType);

            ChangeLockOnParts(postureType);

            OnChangedPosture?.Invoke(postureType);
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
        public void HandleDead()
        {
            if (_isDead) return;

            _isDead = true;
            _bossEnemyAnimator.SetDead();
            OnDead?.Invoke(this);
            Debug.Log("Boss討伐完了");
        }

        #region 鎧関連の処理
        public void InitArmor()
        {
            foreach (var bossArmor in _bossArmorViews)
            {
                bossArmor.Init();
            }
        }

        public void BreakArmor(ArmorAttachmentType attachmentPointsType)
        {
            foreach (var bossArmor in _bossArmorViews)
            {
                if (bossArmor.AttachmentPoints == attachmentPointsType)
                {
                    bossArmor.BreakArmor().Forget();
                }
            }
        }

        public void RepairArmor(ArmorAttachmentType attachmentPointsType = ArmorAttachmentType.None)
        {
            if (attachmentPointsType == ArmorAttachmentType.None)
            {
                foreach (var bossArmor in _bossArmorViews)
                {
                    bossArmor.RepairArmor().Forget();
                }
                return;
            }

            foreach (var bossArmor in _bossArmorViews)
            {
                if (bossArmor.AttachmentPoints == attachmentPointsType)
                {
                    bossArmor.RepairArmor().Forget();
                }
            }
        }
        #endregion

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
        private IBossCharacterAnimationEventReceiver _bossEnemyAnimationEventReceiver;

        // ボスエネミーのController
        private IBossEnemyCharacterController _bossEnemyController;

        // ボスエネミーのAnimator
        private IBossEnemyAnimator _bossEnemyAnimator;

        // 各種マネージャー
        private EffectManager _effectManager;
        private CameraManager _cameraManager;

        // IEnemyを継承した敵クラスが受けることのできるサービス
        private EnemyServices _services;

        // 各種Spawner
        private IAttackHitAreaSpawner _attackHitAreaSpawner = null;

        private bool _isDead = false;
        private bool _isLockable;

        // ボスのタイムスケール
        private ReactiveProperty<float> _timeScale = new(1.0f);

        // ボスの各種パーツ
        private BossCharacterPartsView[] _activeCollisionPartsView;

        // 攻撃のStateMachineBehaviourList
        private List<AttackSMB> _attackSMBList = new List<AttackSMB>();

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

        #region ダメージを受けた際のメソッド群
        private void HandleTakeDamage(DamageContext damageContext, BossCharacterPartsView hitParts, ArmorAttachmentType armorAttachmentType)
        {
            OnTakeDamage?.Invoke(damageContext, hitParts.PartsType, armorAttachmentType);
        }

        private void DamagePopUp(DamageContext damageContext, BossCharacterPartsView hitParts, bool isWeekPoint)
        {
            DamagePopupViewModel damagePopupViewModel;

            damagePopupViewModel = new(DamageSystem.CalculateDamage(damageContext, GetDefenseContext(hitParts)), isWeekPoint, true, hitParts.GetTargetCenter().position);
            OnDamageDealt?.Invoke(damagePopupViewModel);
        }

        private EnemyDefenseContext GetDefenseContext(BossCharacterPartsView bossParts)
        {
            EnemyDefenceType hitPartsDefenseType;
            if (bossParts.Armor == null || bossParts.Armor.IsBroken)
                hitPartsDefenseType = EnemyDefenceType.Flesh;
            else
                hitPartsDefenseType = EnemyDefenceType.Armor;

            EnemyDefenseContext defenseContext = new EnemyDefenseContext()
            {
                EnemyType = hitPartsDefenseType,
                HasShockDebuff = false
            };

            return defenseContext;
        }

        private bool IsHitPartsWeekPoint(BossCharacterPartsView bossParts)
        {
            bool isWeekPoint = false;
            switch (bossParts.PartsType)
            {
                case TakeDamageType.None:
                    break;
                case TakeDamageType.Normal:
                    isWeekPoint = false;
                    break;
                case TakeDamageType.Hard:
                    isWeekPoint = false;
                    break;
                case TakeDamageType.WeekPoint:
                    isWeekPoint = true;
                    break;
                case TakeDamageType.VitalPoint:
                    isWeekPoint = true;
                    break;
            }
            return isWeekPoint;
        }

        private void PlayDamageHitEffect(bool isHitArmor, Vector3 hitPos)
        {
            // 攻撃を受けた際のEffect
            if (isHitArmor) _effectManager.PlayEffect(TAKE_DAMAGE_ARMOR_EFFECT_KEY, hitPos);
            else _effectManager.PlayEffect(TAKE_DAMAGE_EFFECT_KEY, hitPos);
        }
        #endregion

        private void PlayBossSE(string cueName)
        {
            Sound.PlaySE(gameObject, cueName, CueSheetType.Boss);
        }

        private void ChangeLockOnParts(PostureType postureType)
        {
            BossCharacterPartsView[] lockOnOldTargets = null;
            if (_activeCollisionPartsView != null)
            {
                lockOnOldTargets = _activeCollisionPartsView;
                foreach (var oldTarget in _activeCollisionPartsView)
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
                    _activeCollisionPartsView = collision.BossEnemyPartsView;

                    BossCharacterPartsView[] lockOnNewTargets = _activeCollisionPartsView;

                    foreach (var newTarget in _activeCollisionPartsView)
                    {
                        newTarget.SetLockable(true);
                    }

                    OnChangeLockOnParts?.Invoke((lockOnNewTargets, lockOnOldTargets));
                    return;
                }
            }
        }

        #region ボスの姿勢ごとの当たり判定情報
        [Serializable]
        public struct CollisionInformation
        {
            public PostureType CollisionDetectionPostureType => _postureType;

            public Vector3 BossColliderSize => _size;

            public Vector3 BossColliderCenter => _center;

            public BossCharacterPartsView[] BossEnemyPartsView => _bossEnemyPartsView;

            [Header("姿勢")]
            [SerializeField] private PostureType _postureType;

            [Header("当たり判定のCollider情報")]
            [SerializeField] private Vector3 _center;
            [SerializeField] private Vector3 _size;

            [Header("各部位")]
            [SerializeField] private BossCharacterPartsView[] _bossEnemyPartsView;
        }
        #endregion
    }


    #region ボスエネミーの各部位
    [Serializable]
    public class BossCharacterPartsView : ILockOnTarget
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
        public TakeDamageType PartsType => _bossEnemyPartsType;

        public void Init(BossCharacterView bossEnemyView)
        {
            _bossEnemyView = bossEnemyView;

            if (_thisPartsArmer != null) _thisPartsArmer.Init();
        }

        public void SetLockable(bool lockable) => _isLockable = lockable;

        [Header("このPartsのTransform")]
        [SerializeField] private Transform _partsTransform;

        [Header("このパーツに装備するアーマー(なければNullにする)")]
        [SerializeField] private BossArmorView _thisPartsArmer = null;

        [Header("このPartsの硬さ(肉質)")]
        [SerializeField] private TakeDamageType _bossEnemyPartsType;

        private bool _isLockable = false;

        private BossCharacterView _bossEnemyView = null;
    }
    #endregion
}
