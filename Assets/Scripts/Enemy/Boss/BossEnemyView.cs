using BossEnemy.Data;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// ボス本体のViewClass
/// </summary>
public class BossEnemyView : MonoBehaviour, IEnemy, IPoolable
{
    // --- Events ---

    /// <summary>HP変化時に発火するイベント（current, max）</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>ダメージを受けた際にポップアップ情報を通知するイベント</summary>
    public event Action<DamagePopupViewModel> OnDamageDealt;

    /// <summary>ダメージを受けて生存したときに発火するイベント（被弾入れ替え判定に使用）</summary>
    public event Action<IEnemy> OnDamaged;

    /// <summary>死亡時に発火するイベント</summary>
    public event Action<IEnemy> OnDead;

    // --- Properties ---

    /// <summary> BossEnemyと内部Modelを繋ぐControllerClass </summary>
    public BossEnemyController BossEnemyController => _bossEnemyController;

    /// <summary>ConditionController への参照</summary>
    public IEnemyConditionController ConditionController { get; }

    /// <summary>EnemyAnimator への参照</summary>
    public IEnemyAnimator EnemyAnimator { get; }

    /// <summary> 自身のTransformへの参照 </summary>
    public Transform Self => transform;

    /// <summary>インスタンス識別ID（AttackerSlotのキーに使用）</summary>
    public int Id { get; }

    /// <summary>ボス判定</summary>
    public bool IsBoss => true;

    /// <summary>HitStop等で使用するタイムスケール（DeadCondition の物理スケーリングに使用）</summary>
    public float TimeScale { get; }

    /// <summary> 死亡判定 </summary>
    public bool IsDead => _isDead;

    /// <summary> ロックオン可能か(非アクティブ状態でオフにしたい場合など)。 </summary>
    public bool IsLockable => _isLockable;

    // --- Methods ---

    /// <summary> 初期化する </summary>
    public void Init()
    {
        _isLockable = true;
        _bossEnemyController.Init(_services);
    }

    /// <summary>攻撃の内容を渡して内部でダメージ計算をする</summary>
    public void TakeDamage(DamageContext context)
    {
        BossEnemyPartsType partsType = BossEnemyPartsType.None;

        // 攻撃から一番近いボスエネミーのパーツを割り出す
        float saveClosestDistance = 1000;
        foreach (var bossParts in _bossEnemyPartsView)
        {
            float playerDistance = _services.PlayerInformationService.ToPlayerDistance(bossParts.PartsPosition);

            if (playerDistance < saveClosestDistance)
            {
                saveClosestDistance = playerDistance;
                partsType = bossParts.BossEnemyPartsType;
            }
        }

        _bossEnemyController.HandleDamaged(context, partsType);

        OnDamaged.Invoke(this);
    }

    /// <summary>ノックバックの力を与える</summary>
    public void AddKnockbackForce(Vector3 direction)
    {

    }

    /// <summary>ConditionによりActionを阻害する</summary>
    public void OnConditionInterrupt()
    {

    }

    /// <summary>位置を直接セットする</summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
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
        OnDead.Invoke(this);
    }

    [Header("BossEnemyのController")]
    [SerializeField, Tooltip("BossEnemyのViewとModelの仲介役")] 
    private BossEnemyController _bossEnemyController;

    [Header("ボスエネミーの各部位のView")]
    [SerializeField] private BossEnemyPartsView[] _bossEnemyPartsView;

    [Header("ロックオン対象のTransform")]
    [SerializeField] private Transform _targetCenterTransform;

    private EnemyServices _services;

    private bool _isDead;
    private bool _isLockable;

    private void Update()
    {
        if (_bossEnemyController == null) return;

        if (!_isDead) 
            _bossEnemyController.OnUpdate();
    }

    #region ボスの装備するアーマークラス
    /// <summary> ボスの装備するアーマー </summary>
    public class BossArmerView : MonoBehaviour
    {
        public void Init(BossEnemyView bossEnemyView)
        {
            _bossEnemyView = bossEnemyView;
        }

        /// <summary> アーマー破壊時の処理 </summary>
        public async UniTask ArmerBreak()
        {

        }
        
        private BossEnemyView _bossEnemyView = null;
    }
    #endregion

    #region ボスエネミーの各部位
    [Serializable]
    public class BossEnemyPartsView : ILockOnTarget
    {
        /// <summary>
        /// ロックオンなどの中心のTransformを取得する
        /// </summary>
        public Transform GetTargetCenter() => _partsTransform;

        /// <summary>
        /// ロックオン可能か(非アクティブ状態でオフにしたい場合など)。
        /// </summary>
        public bool IsLockable => _bossEnemyView.IsLockable;

        /// <summary>
        /// パーツの座標
        /// </summary>
        public Vector3 PartsPosition => _partsTransform.position;

        /// <summary>
        /// このパーツにつけるアーマー
        /// </summary>
        public BossArmerView ThisPartsArmer => _thisPartsArmer;

        /// <summary>
        /// このパーツの硬さ(肉質)
        /// </summary>
        public BossEnemyPartsType BossEnemyPartsType => _bossEnemyPartsType;

        public void Init(BossEnemyView bossEnemyView)
        {
            _bossEnemyView = bossEnemyView;
        }

        [Header("このPartsのTransform")]
        [SerializeField] private Transform _partsTransform;

        [Header("このパーツに装備するアーマー(なければNullにする)")]
        [SerializeField] private BossArmerView _thisPartsArmer = null;

        [Header("このPartsの硬さ(肉質)")]
        [SerializeField] private BossEnemyPartsType _bossEnemyPartsType;

        private BossEnemyView _bossEnemyView = null;
    }
    #endregion
}
