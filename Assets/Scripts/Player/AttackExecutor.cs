using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    /// <summary> ヒット結果をサウンドハンドラーなどに通知するイベント </summary>
    public event Action<HitSoundContext> OnHitResultReady;

    /// <summary> スイング音通知用。攻撃判定が出る瞬間に発火する </summary>
    public event Action<PlayerMode> OnSwingReady;

    public void Init(IPlayerStats stats, SkillManager manager)
    {
        _playerStats = stats;
        _skillManager = manager;
    }

    public void Execute(AttackData data, AttackInput input, ModeData modeData)
    {
        OnSwingReady?.Invoke(data.Mode);

        _lastAttackData = data;

        var attackPos = transform.position + transform.forward * data.AttackRange;
        var cols = Physics.OverlapSphere(attackPos, data.AttackRadius, _layer);

        Debug.Log($"{data.Mode}：{data.AttackName}で攻撃");

        var context = new AttackContext(data.Mode, attackPos, transform)
        {
            AttackPower = _playerStats.AttackPower * data.DamageMultiplier * modeData.AttackMultiplier,
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

        var applicableSkills = GetApplicableSkills(context, data);
        ApplySkills(ref context, applicableSkills);
        context.OnBeforeAttack?.Invoke();

        bool hasHitResult = false;
        bool isWeakPoint = false;
        bool isArmorBreak = false;
        bool isKill = false;
        bool isArmorHit = false; 
        var hitEnemyTargets = new List<ISpeedChange>();

        foreach (var col in cols)
        {
            if (!col.TryGetComponent(out IEnemy enemy)) continue;

            var perHitContext = context;
            RollCritical(ref perHitContext, modeData);
            perHitContext.OnHit?.Invoke(enemy);

            var damageContext = BuildDamageContext(perHitContext);

            damageContext.OnHitResult = result =>
            {
                hasHitResult = true;
                if (result.IsWeakPoint) isWeakPoint = true;
                if (result.IsArmorBreak) isArmorBreak = true;
                if (result.IsKill) isKill = true;
                if (result.IsArmorHit) isArmorHit = true; 
                if (enemy is ISpeedChange speedChange)
                    hitEnemyTargets.Add(speedChange);
            };

            enemy.TakeDamage(damageContext);
        }

        // 地面ヒット音（特定攻撃のみ）
        if (data.PlayGroundHitSE)
            Sound.PlayTousnSE(gameObject, SoundCueNames.Tousin.GroundHit);

        if (hasHitResult)
        {
            // HitStop
            if (ServiceLocator.TryGet(out HitStopManager hitStop))
            {
                hitStop.Trigger(
                    data: data.HitStopData,
                    isWeakPoint: isWeakPoint,
                    isArmorBreak: isArmorBreak,
                    isKill: isKill,
                    hitEnemyTargets: hitEnemyTargets
                );
            }

            // サウンド通知
            OnHitResultReady?.Invoke(new HitSoundContext
            {
                IsKill = isKill,
                IsArmorBreak = isArmorBreak,
                IsWeakPoint = isWeakPoint,
                IsArmorHit = isArmorHit,
                PlayerMode = data.Mode,
            });
        }

        context.OnAfterAttack?.Invoke();
    }

    [SerializeField] private LayerMask _layer;
    private IPlayerStats _playerStats;
    private SkillManager _skillManager;

    private List<SkillBase> GetApplicableSkills(AttackContext context, AttackData data)
    {
        if (_skillManager == null) return new List<SkillBase>();
        return _skillManager.GetAttackSkills()
            .Where(skill => skill.CanApply(context, data))
            .OrderByDescending(skill => skill.Priority)
            .ToList();
    }

    private void ApplySkills(ref AttackContext context, List<SkillBase> skills)
    {
        foreach (var skill in skills) skill.Apply(ref context);
    }

    private void RollCritical(ref AttackContext context, ModeData data)
    {
        context.IsCritical = false;
        context.CriticalMultiplier = 1f;
        if (UnityEngine.Random.value < _playerStats.CriticalRate)
        {
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

    private AttackData _lastAttackData;
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_lastAttackData == null) return;
        Gizmos.color = Color.red;
        var pos = transform.position + transform.forward * _lastAttackData.AttackRange;
        Gizmos.DrawWireSphere(pos, _lastAttackData.AttackRadius);
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
    public readonly PlayerMode PlayerMode;

    // 攻撃の座標
    public readonly Vector3 AttackPosition;
    public readonly Transform PlayerTransform;

    public bool IsCritical;
    public float CriticalMultiplier;

    public KnockbackContext? Knockback;

    /// <summary>攻撃開始直前</summary>
    public Action OnBeforeAttack;

    /// <summary>攻撃終了直後</summary>
    public Action OnAfterAttack;

    /// <summary>敵にヒットした瞬間</summary>
    public Action<IEnemy> OnHit;

    /// <summary>感電</summary>
    public ElectricShock ElectricShock;

    public AttackContext(PlayerMode mode, Vector3 attackPos, Transform playerTransform)
    {
        PlayerMode = mode;
        AttackPosition = attackPos;
        PlayerTransform = playerTransform;

        AttackPower = 0;
        IsCritical = false;
        CriticalMultiplier = 1f;
        Knockback = null;
        OnBeforeAttack = null;
        OnAfterAttack = null;
        OnHit = null;
        ElectricShock = new();
    }
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

    /// <summary>
    /// 攻撃がヒットした瞬間に発動するイベント。HitResultでヒットの結果を受け取ることができる。
    /// </summary>
    public Action<HitResult> OnHitResult;
}

/// <summary>
/// ノックバックの方向や威力などの情報
/// </summary>
public struct KnockbackContext
{
    public Vector3 Direction;
    public float Power;
    public float Upward;
}

/// <summary>
/// 攻撃がヒットした際の結果に関する情報
/// </summary>
public struct HitResult
{
    public bool IsKill;
    public bool IsArmorBreak;
    public bool IsWeakPoint;
    public bool IsArmorHit; 
}

/// <summary>
/// 攻撃のヒット結果をサウンドハンドラーなどに通知するための情報
/// </summary>
public struct HitSoundContext
{
    public bool IsKill;
    public bool IsArmorBreak;
    public bool IsWeakPoint;
    public bool IsArmorHit;
    public PlayerMode PlayerMode;
}
