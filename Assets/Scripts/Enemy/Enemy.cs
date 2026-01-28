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

        _stats.TakeDamage(context.Damage);
    }

    [SerializeField] protected EnemyData _data;
    [SerializeField] private Transform _targetCenter;

    protected EnemyStats _stats;
    protected Transform _playerTransform;

    protected bool _isDead; // 軽い実装のため bool のフラグを使用

    protected virtual void Awake()
    {
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
