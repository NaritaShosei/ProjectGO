using System;
using UnityEngine;

/// <summary>
/// ボス戦で使用する攻撃可能な核オブジェクト
/// IEnemyを実装し、受けたダメージを_boss（TestBoss）へ中継するプロキシとして機能する
/// Behaviour・Condition・サービス注入は不要のためすべてノーオペレーション
/// </summary>
public sealed class BossCore : MonoBehaviour, IEnemy
{
    /// <summary>_bossのOnDeadをそのまま中継する</summary>
    public event Action<IEnemy> OnDead
    {
        add => _boss.OnDead += value;
        remove => _boss.OnDead -= value;
    }

    /// <summary>_bossのOnHealthChangedをそのまま中継する</summary>
    public event Action<float, float> OnHealthChanged
    {
        add => _boss.OnHealthChanged += value;
        remove => _boss.OnHealthChanged -= value;
    }

    /// <summary>_bossのOnDamageDealtをそのまま中継する</summary>
    public event Action<DamagePopupViewModel> OnDamageDealt
    {
        add => _boss.OnDamageDealt += value;
        remove => _boss.OnDamageDealt -= value;
    }

    /// <summary>_bossのOnDamagedをそのまま中継する</summary>
    public event Action<IEnemy> OnDamaged
    {
        add => _boss.OnDamaged += value;
        remove => _boss.OnDamaged -= value;
    }

    public IEnemyConditionController ConditionController => _nullConditionController;

    /// <summary>BossCoreはAnimatorを持たないためNull Objectを返す</summary>
    public IEnemyAnimator EnemyAnimator => _nullAnimator;

    public int Id => GetInstanceID();
    public bool IsBoss => true;
    public Vector3 Position => transform.position;
    public float TimeScale => 1f;

    public bool IsDead => _boss.IsDead;

    public bool IsLockable => true;

    public void InjectServices(EnemyServices services) { }
    public void Init(IPlayer player) { }
    public void OnConditionInterrupt() { }
    public void AddKnockbackForce(Vector3 direction) { }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// サンダーモードの攻撃のみ受け付け、_damageMultiplierをかけてボス本体へ中継する
    /// TODO:雑にボスにダメージを与える橋渡しになっているため、仕様によって変更の余地
    /// </summary>
    public void TakeDamage(DamageContext context)
    {
        if (context.PlayerMode != PlayerMode.Thunder) return;

        _boss.TakeDamage(new DamageContext
        {
            AttackPower = context.AttackPower * _damageMultiplier,
            PlayerMode = context.PlayerMode,
            OnHitResult = context.OnHitResult,
            IsCritical = context.IsCritical,
            Knockback = context.Knockback,
            ElectricShock = context.ElectricShock
        });
    }

    [SerializeField] private TestBoss _boss;
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private Transform _targetCenter;

    private readonly IEnemyAnimator _nullAnimator = new NullEnemyAnimator();
    private readonly IEnemyConditionController _nullConditionController = new NullEnemyConditionController();
}
