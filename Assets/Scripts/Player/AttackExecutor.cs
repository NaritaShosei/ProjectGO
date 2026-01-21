using System;
using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    // デバッグ用
    [SerializeField] private SkillBase skill;
    public void Init(float power)
    {
        _attackPower = power;
    }

    /// <summary>
    /// 与えられたデータを基に攻撃
    /// </summary>
    public void Execute(AttackData data, AttackInput input, ModeData modeData)
    {
        _lastAttackData = data;
        // TODO:クリティカルがない
        var attackPos = transform.position + transform.forward * data.AttackRange;
        var cols = Physics.OverlapSphere(attackPos, data.AttackRadius, _layer);

        Debug.Log($"{data.AttackName}で攻撃");

        var context = new AttackContext
        {
            Damage = _attackPower * data.DamageMultiplier * modeData.AttackMultiplier,
            PlayerMode = data.Mode
        };

        // スキルの検索等はここで
        var skillContext = skill.Apply(context);

        // 攻撃直前スキルを発動
        skillContext.OnBeforeAttack?.Invoke();

        foreach (var col in cols)
        {
            if (col.TryGetComponent(out IEnemy enemy))
            {
                // ヒットの瞬間(敵毎)スキルを発動
                skillContext.OnHit?.Invoke();

                enemy.TakeDamage(context);
            }
        }

        // 攻撃直後スキルの発動
        skillContext.OnAfterAttack?.Invoke();
    }

    private float _attackPower;

    [SerializeField] private LayerMask _layer;

    // デバッグ用
    private AttackData _lastAttackData;
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
public struct AttackContext
{
    public float Damage;
    public PlayerMode PlayerMode;

    /// <summary>攻撃開始直前</summary>
    public Action OnBeforeAttack;

    /// <summary>攻撃終了直後</summary>
    public Action OnAfterAttack;

    /// <summary>敵にヒットした瞬間</summary>
    public Action OnHit;
}
