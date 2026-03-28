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
        // 攻撃力を一定割合増加
        context.AttackPower *= (1f + _boostAmount);

        var ct = context;
        context.OnAfterAttack += () => SpawnEffect(ct.AttackPosition);
        context.OnAfterAttack += () => Skill(ct);
    }

    [Header("条件設定")]
    [SerializeField] private float _boostAmount = 0.5f;
    [SerializeField] private int _requiredComboIndex = 2;
    [SerializeField] private AttackType _targetAttackType = AttackType.LightAttack;

    [Header("攻撃設定")]
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private float _damageRadius = 2f;
    [SerializeField] private float _damageMultiplier = 1.5f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private KnockbackContext _knockbackContext;

    private void SpawnEffect(Vector3 position)
    {
        if (_effectPrefab != null)
        {
            GameObject.Instantiate(_effectPrefab, position, Quaternion.identity);
        }
    }

    private void Skill(AttackContext context)
    {
        // コンテキストから情報をもらい一定範囲の敵にダメージを与える
        var cols = Physics.OverlapSphere(context.AttackPosition, _damageRadius, _enemyLayer);

        foreach (var col in cols)
        {
            if (col.TryGetComponent(out IEnemy enemy))
            {
                _knockbackContext.Direction = (enemy.Position - context.AttackPosition).normalized;

                var damageContext = new DamageContext
                {
                    AttackPower = context.AttackPower * _damageMultiplier,
                    PlayerMode = context.PlayerMode,
                    IsCritical = false,
                    OnHitResult = null,
                    Knockback = _knockbackContext
                };
                enemy.TakeDamage(damageContext);
            }
        }
    }
}
