using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    public void Init(IPlayerStats stats, SkillManager manager)
    {
        _playerStats = stats;
        _skillManager = manager;
    }

    /// <summary>
    /// 与えられたデータを基に攻撃
    /// </summary>
    public void Execute(AttackData data, AttackInput input, ModeData modeData)
    {
        _lastAttackData = data;

        var attackPos = transform.position + transform.forward * data.AttackRange;
        var cols = Physics.OverlapSphere(attackPos, data.AttackRadius, _layer);

        Debug.Log($"{data.Mode}：{data.AttackName}で攻撃");

        var context = new AttackContext
        {
            AttackPower = _playerStats.AttackPower * data.DamageMultiplier * modeData.AttackMultiplier,
            PlayerMode = data.Mode
        };

        if (data.EnableKnockback)
        {
            context.Knockback = new KnockbackContext
            {
                Direction = transform.forward,
                Power = data.KnockbackPower,
                Upward = data.KnockbackUpward
            };
        }

        // 取得済みスキルの中から条件に合うものを取得して適用
        var applicableSkills = GetApplicableSkills(context, data);
        ApplySkills(ref context, applicableSkills);

        // 攻撃直前スキルを発動
        context.OnBeforeAttack?.Invoke();

        foreach (var col in cols)
        {
            if (col.TryGetComponent(out IEnemy enemy))
            {
                var perHitContext = context; // コピー
                RollCritical(ref perHitContext, modeData);

                perHitContext.OnHit?.Invoke();

                var damageContext = BuildDamageContext(perHitContext);
                enemy.TakeDamage(damageContext);
            }
        }

        // 攻撃直後スキルの発動
        context.OnAfterAttack?.Invoke();
    }


    [SerializeField] private LayerMask _layer;
    private IPlayerStats _playerStats;
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

        return _skillManager.GetAttackSkills()
            .Where(skill => skill.CanApply(context, data))
            .OrderByDescending(skill => skill.Priority)
            .ToList();
    }

    /// <summary>
    /// 複数のスキルを順番に適用
    /// </summary>
    private void ApplySkills(ref AttackContext context, List<SkillBase> skills)
    {
        foreach (var skill in skills)
        {
            skill.Apply(ref context);
        }
    }

    private void RollCritical(ref AttackContext context, ModeData data)
    {
        float chance = _playerStats.CriticalRate;
        context.IsCritical = false;
        context.CriticalMultiplier = 1f;

        if (UnityEngine.Random.value < chance)
        {
            // クリティカル耐性の可能性を考え、ここではクリティカルダメージを求めない
            context.IsCritical = true;
            context.CriticalMultiplier = data.CriticalDamageMultiplier;
        }
    }

    private DamageContext BuildDamageContext(AttackContext context)
    {
        return new DamageContext
        {
            AttackPower = context.AttackPower,
            PlayerMode = context.PlayerMode,
            CriticalMultiplier = context.CriticalMultiplier,
            IsCritical = context.IsCritical,
            ElectricShock = context.ElectricShock,
            Knockback = context.Knockback
        };
    }

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

    public bool IsCritical;
    public float CriticalMultiplier;

    public KnockbackContext? Knockback;

    /// <summary>攻撃開始直前</summary>
    public Action OnBeforeAttack;

    /// <summary>攻撃終了直後</summary>
    public Action OnAfterAttack;

    /// <summary>敵にヒットした瞬間</summary>
    public Action OnHit;

    /// <summary>感電</summary>
    public ElectricShock ElectricShock;
}

/// <summary>
/// Enemyが攻撃を受ける際に扱う情報
/// </summary>
public struct DamageContext
{
    public float AttackPower;
    public PlayerMode PlayerMode;

    public bool IsCritical;
    public float CriticalMultiplier;

    public ElectricShock ElectricShock;
    /// <summary>
    /// HasValueで攻撃にノックバック効果があるかを確認。
    /// Valueでノックバックの方向や威力を取得可能。
    /// </summary>
    public KnockbackContext? Knockback;
}

public struct KnockbackContext
{
    public Vector3 Direction;
    public float Power;
    public float Upward;
}
