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
    }

    /// <summary>攻撃の内容を渡して内部でダメージ計算をする</summary>
    public void TakeDamage(DamageContext context)
    {
        Debug.Log("ダメージを受けた！");

        _bossEnemyController.HandleDamaged(context);

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
        return transform;
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

    [Header("BossEnemyのController")]
    [SerializeField, Tooltip("BossEnemyのViewとModelの仲介役")] 
    private BossEnemyController _bossEnemyController;

    [Header("ボスが装着する各ArmerのView")]
    [SerializeField, Tooltip("右手Armer")] private BossArmerView _rightArmArmer;
    [SerializeField, Tooltip("左手Armer")] private BossArmerView _leftArmArmer;
    [SerializeField, Tooltip("右足Armer")] private BossArmerView _rightLegArmer;
    [SerializeField, Tooltip("左足Armer")] private BossArmerView _leftLegArmer;

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
    public class BossArmerView : ILockOnTarget
    {
        /// <summary>
        /// ロックオンなどの中心のTransformを取得する
        /// </summary>
        public Transform GetTargetCenter() => _targetCenter;

        /// <summary>
        /// ロックオン可能か(非アクティブ状態でオフにしたい場合など)。
        /// </summary>
        public bool IsLockable => _bossEnemyView.IsLockable;

        public void Init(BossEnemyView bossEnemyView)
        {
            _bossEnemyView = bossEnemyView;
        }

        /// <summary> アーマー破壊時の処理 </summary>
        public async UniTask ArmerBreak()
        {

        }

        [Header("ロックオンなどの中心のTransformを取得する")]
        [SerializeField]private Transform _targetCenter = null;

        private BossEnemyView _bossEnemyView = null;
    }
    #endregion
}
