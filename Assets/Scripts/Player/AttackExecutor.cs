using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    public void Init(float power, SkillManager manager)
    {
        _attackPower = power;
        _skillManager = manager;
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

        Debug.Log($"{data.Mode}：{data.AttackName}で攻撃");

        var context = new AttackContext
        {
            AttackPower = _attackPower * data.DamageMultiplier * modeData.AttackMultiplier,
            PlayerMode = data.Mode
        };

        // 取得済みスキルの中から条件に合うものを取得して適用
        var applicableSkills = GetApplicableSkills(context, data);
        var damageContext = ApplySkills(ref context, applicableSkills);

        // 攻撃直前スキルを発動
        context.OnBeforeAttack?.Invoke();

        foreach (var col in cols)
        {
            if (col.TryGetComponent(out IEnemy enemy))
            {
                // ヒットの瞬間(敵毎)スキルを発動
                context.OnHit?.Invoke();

                enemy.TakeDamage(damageContext);
            }
        }

        // 攻撃直後スキルの発動
        context.OnAfterAttack?.Invoke();
    }

    private float _attackPower;
    private SkillManager _skillManager;

    /// <summary>
    /// 条件に合う取得済みスキルを優先度順に取得
    /// </summary>
    private List<SkillBase> GetApplicableSkills(AttackContext context, AttackData data)
    {
        if (_skillManager == null)
        {
            return new List<SkillBase>();
        }

        return _skillManager.GetOwnedSkills()
            .Where(skill => skill.CanApply(context, data))
            .OrderByDescending(skill => skill.Priority)
            .ToList();
    }

    /// <summary>
    /// 複数のスキルを順番に適用
    /// </summary>
    private DamageContext ApplySkills(ref AttackContext context, List<SkillBase> skills)
    {
        var damageContext = new DamageContext
        {
            AttackPower = context.AttackPower,
            PlayerMode = context.PlayerMode,
        };

        foreach (var skill in skills)
        {
            // 各スキルが前のスキルの結果を受け取って処理
            context.AttackPower = damageContext.AttackPower;
            damageContext = skill.Apply(ref context);
        }

        return damageContext;
    }

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

/// <summary>
/// Playerの攻撃やスキルに扱う情報
/// </summary>
public struct AttackContext
{
    public float AttackPower;
    public PlayerMode PlayerMode;

    /// <summary>攻撃開始直前</summary>
    public Action OnBeforeAttack;

    /// <summary>攻撃終了直後</summary>
    public Action OnAfterAttack;

    /// <summary>敵にヒットした瞬間</summary>
    public Action OnHit;
}

/// <summary>
/// Enemyが攻撃を受ける際に扱う情報
/// </summary>
public struct DamageContext
{
    public float AttackPower;
    public PlayerMode PlayerMode;
}