using System;
using UnityEngine;

/// <summary>
/// Enemyの基底クラス
/// </summary>
public abstract class Enemy : MonoBehaviour, IEnemy
{
    public event Action<IEnemy> OnDead;

    public virtual void Init(IPlayer player)
    {
        _playerTransform = player.GetTargetCenter();
    }

    public void AddKnockBackForce(Vector3 direction)
    {
        // ノックバック
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public virtual void TakeDamage(DamageContext context)
    {
        if (_isDead) { return; }

        int damage = DamageSystem.Calculate(context, _defenceContext);

        _stats.TakeDamage(damage);

        // TODO: スタン攻撃かDamageContextから判別
        // TODO: スタン攻撃ならEnemyContextにおいてスタン状態に変更
        // TODO: EnemyクラスからEnemyContextへの参照がない
        // TODO: ひとまず一つ下のStunEnemyのほうでoverrideして実装しよう
    }

    [SerializeField] protected EnemyData _data;
    [SerializeField] private Transform _targetCenter;

    protected DefenseContext _defenceContext;
    protected EnemyStats _stats;
    protected Transform _playerTransform;

    protected bool _isDead; // 軽い実装のため bool のフラグを使用

    protected virtual void Awake()
    {
        // 雑に生身限定
        _defenceContext = new DefenseContext()
        {
            EnemyType = EnemyType.Flesh,
        };

        _stats = new EnemyStats(_data);

        _stats.OnHealthZero += _stats.Kill;

        _stats.OnDead += OnDeath;
    }
    protected virtual void Update()

    {
        if (_isDead) { return; }
        UpdateEnemy(Time.deltaTime);
    }

    private void OnDestroy()
    {
        _stats.OnHealthZero -= _stats.Kill;

        _stats.OnDead -= OnDeath;
    }

    /// <summary>
    /// 死亡時処理
    /// </summary>
    protected virtual void OnDeath()
    {
        if (_isDead) { return; }

        _isDead = true;
        OnDead?.Invoke(this);
        OnDeathInternal();
    }

    protected virtual void OnDeathInternal()
    {
        Destroy(gameObject);
    }

    protected abstract void UpdateEnemy(float deltaTime);

}

public struct DefenseContext
{
    public EnemyType EnemyType; // 鎧 / 生身
}

public enum EnemyType
{
    [InspectorName("生身")]
    Flesh,
    [InspectorName("鎧")]
    Armor,
}