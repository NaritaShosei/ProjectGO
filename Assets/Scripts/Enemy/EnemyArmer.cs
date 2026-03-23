using System;
using UnityEngine;

/// <summary>
/// ボス戦で使用する破壊可能な部位オブジェクト
/// IEnemyを実装するが、Behaviour・Condition・サービス注入は不要のためすべてノーオペレーション
/// HPがゼロになると破壊され、_coreオブジェクトをアクティブにしてから自身を非表示にする
/// </summary>
public sealed class EnemyArmer : MonoBehaviour, IEnemy
{
    public event Action<IEnemy> OnDead;
    public event Action<float, float> OnHealthChanged;
    public event Action<DamagePopupViewModel> OnDamageDealt;

    public IEnemyConditionController ConditionController => _nullConditionController;

    /// <summary>EnemyArmerはAnimatorを持たないためNull Objectを返す</summary>
    public IEnemyAnimator EnemyAnimator => _nullAnimator;

    public int Id => GetInstanceID();
    public bool IsBoss => false;
    public Vector3 Position => transform.position;

    /// <summary>HPがゼロ以下になったかどうかを返す</summary>
    public bool IsBroken => _hp <= 0;

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
    /// ウォリアーモードの攻撃のみ受け付ける
    /// HPがゼロになった場合はOnDeadを発火してBreakを実行する
    /// </summary>
    public void TakeDamage(DamageContext context)
    {
        if (context.PlayerMode != PlayerMode.Warrior) return;

        float beforeHp = _hp;
        _hp -= context.AttackPower;
        OnHealthChanged?.Invoke(beforeHp, _hp);

        InvokeOnDamageDealt((int)context.AttackPower, isWeakPoint: false, context.IsCritical);

        if (_hp <= 0)
        {
            OnDead?.Invoke(this);
            Break();

            context.OnHitResult?.Invoke(new HitResult
            {
                IsKill = false,
                IsArmorBreak = true,
                IsWeakPoint = false
            });
        }
    }

    /// <summary>
    /// ダメージポップアップ表示用のイベントを発火する
    /// </summary>
    public void InvokeOnDamageDealt(int damage, bool isWeakPoint, bool isCritical)
    {
        OnDamageDealt?.Invoke(new DamagePopupViewModel(
            damage: damage,
            isWeakPoint: isWeakPoint,
            isCritical: isCritical,
            worldPosition: GetTargetCenter().position
        ));
    }

    [SerializeField] private float _hp = 50;
    [SerializeField] private GameObject _core;
    [SerializeField] private Transform _targetCenter;

    private readonly IEnemyAnimator _nullAnimator = new NullEnemyAnimator();
    private readonly IEnemyConditionController _nullConditionController = new NullEnemyConditionController();

    /// <summary>
    /// _coreをアクティブにして自身を非表示にする
    /// </summary>
    private void Break()
    {
        if (_core) _core.SetActive(true);
        gameObject.SetActive(false);
    }
}
