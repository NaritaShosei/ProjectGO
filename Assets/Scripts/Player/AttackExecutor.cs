using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    public void Init(float power)
    {
        _attackPower = power;
    }

    /// <summary>
    /// 与えられたデータを基に攻撃
    /// </summary>
    public void Execute(AttackData_main data, AttackInput input)
    {
        _lastAttackData = data;
        // TODO:クリティカルがない
        var attackPos = transform.position + transform.forward * data.AttackRange;
        var cols = Physics.OverlapSphere(attackPos, data.AttackRadius, _layer);

        Debug.Log($"{data.AttackName}で攻撃");

        foreach (var col in cols)
        {
            if (col.TryGetComponent(out IEnemy enemy))
            {
                enemy.TakeDamage(_attackPower * data.DamageMultiplier);
            }
        }
    }

    private float _attackPower;

    [SerializeField] private LayerMask _layer;

    // デバッグ用
    private AttackData_main _lastAttackData;
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_lastAttackData == null) return;

        Gizmos.color = Color.red;
        var pos = transform.position + transform.forward * _lastAttackData.AttackRange;
        Gizmos.DrawWireSphere(pos, _lastAttackData.AttackRadius);

        // 向き確認用
        Gizmos.DrawLine(transform.position, pos);
    }
#endif
}
