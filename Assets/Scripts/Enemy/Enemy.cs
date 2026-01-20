using System;
using UnityEngine;

// NOTE:
// この GoblinEnemy は「基盤用の最小実装」です。
// ・複雑なAI
// ・スキル
// ・状態遷移
// は意図的に入れていません。
// 拡張する場合はこのクラスを参考に派生 or 分離してください。

public abstract class Enemy : MonoBehaviour, IEnemy
{
    public event Action<IEnemy> OnDead;

    public void AddKnockBackForce(Vector3 direction)
    {
        // ノックバック
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public virtual void TakeDamage(AttackContext context)
    {
        if (_isDead) { return; }

        _currentHP -= context.Damage;

        if (_currentHP <= 0f)
        {
            OnDeath();
        }
    }

    [SerializeField] protected EnemyData _data;
    [SerializeField] private Transform _targetCenter;
    protected float _currentHP;

    protected bool _isDead; // 軽い実装のため bool のフラグを使用

    protected virtual void Awake()
    {
        _currentHP = _data.MaxHP;
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

    protected virtual void Update()
    {
        if (_isDead) { return; }
        UpdateEnemy(Time.deltaTime);
    }
}
