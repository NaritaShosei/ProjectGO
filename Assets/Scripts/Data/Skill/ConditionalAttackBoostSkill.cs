using Cysharp.Threading.Tasks;
using PixPlays.ElementalVFX;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ConditionalAttackBoostSkill", menuName = "GameData/Skill/ConditionalAttackBoostSkill")]

public class ConditionalAttackBoostSkill : SkillBase
{
    public override bool CanApply(AttackContext context, AttackData data)
    {
        // 攻撃タイプが一致しているか
        bool isTargetAttackType = data.AttackType == _targetAttackType;

        // 必要なコンボ数に到達しているか
        bool hasRequiredComboCount = data.ComboIndex >= _requiredComboIndex;

        // コンボの最終段かどうか
        bool isLastCombo = data.NextComboAttackId == -1;

        bool isWarriorMode = context.PlayerMode == PlayerMode.Warrior;

        return isTargetAttackType
            && hasRequiredComboCount
            && isLastCombo
            && isWarriorMode;
    }

    public override void Apply(ref AttackContext context)
    {
        context.AttackPower *= (1f + _boostAmount);

        Vector3 groundPosition =
            GetGroundPosition(context.AttackPosition);

        var ct = context;

        context.OnAfterAttack += () => SpawnEffect(groundPosition);

        context.OnAfterAttack += () =>
            Skill(ct, groundPosition);
    }

    [Header("条件設定")]
    [SerializeField] private float _boostAmount = 0.5f;
    [SerializeField] private int _requiredComboIndex = 2;
    [SerializeField] private AttackType _targetAttackType = AttackType.LightAttack;

    [Header("攻撃設定")]
    [SerializeField] private string _effectKey;
    [SerializeField] private float _damageRadius = 2f;
    [SerializeField] private float _damageMultiplier = 1.5f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private KnockbackContext _knockbackContext;

    private Vector3 GetGroundPosition(Vector3 position)
    {
        position.y = 0f;
        return position;
    }

    private void SpawnEffect(Vector3 position)
    {

        if (string.IsNullOrEmpty(_effectKey)) return;

        if (ServiceLocator.TryGet(out EffectManager effectManager))
        {
            effectManager.PlayEffect(
                _effectKey,
                position);
        }
    }

    private void Skill(
    AttackContext context,
    Vector3 attackPosition)
    {
        if (!ServiceLocator.TryGet(out EnemyManager enemyManager))
            return;

        var enemies =
            enemyManager.GetEnemiesInRange(
                attackPosition,
                _damageRadius);

        foreach (var enemy in enemies)
        {
            var knockback = _knockbackContext;

            knockback.Direction =
                (enemy.Position - attackPosition).normalized;

            var damageContext = new DamageContext
            {
                AttackPower =
                    context.AttackPower * _damageMultiplier,

                PlayerMode = context.PlayerMode,

                IsCritical = false,

                OnHitResult = null,

                Knockback = knockback
            };

            enemy.TakeDamage(damageContext);
        }
    }
}
