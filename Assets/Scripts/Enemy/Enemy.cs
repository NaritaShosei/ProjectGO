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
    public EnemyData Data => _enemyData;
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
        _currentHP -= context.Damage;

        if (_currentHP <= 0f)
        {
            OnDeath();
        }
    }

    [SerializeField] private EnemyData _enemyData;
    [SerializeField] private Transform _targetCenter;
    protected float _currentHP;

    protected virtual void Awake()
    {
        _currentHP = _enemyData.MaxHP;
    }

    protected virtual void OnDeath()
    {
        Destroy(gameObject);
    }

    protected abstract void UpdateEnemy(float deltaTime);

    protected virtual void Update()
    {
        UpdateEnemy(Time.deltaTime);
    }
}
