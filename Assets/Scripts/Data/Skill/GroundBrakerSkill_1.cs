using UnityEngine;

[CreateAssetMenu(fileName = "GroundBrakerSkill_1", menuName = "GameData/Skill/GroundBrakerSkill_1")]
public class GroundBrakerSkill_1 : SkillBase
{
    public override void Apply(ref AttackContext context)
    {
        var ct = context;
        context.OnAfterAttack += () => SpawnEffect(ct.AttackPosition);
        context.OnAfterAttack += () => Skill(ct);
    }

    public override bool CanApply(AttackContext context, AttackData data)
    {
        bool isWarriorMode = context.PlayerMode == PlayerMode.Warrior;
        bool isLastCombo = data.ComboIndex == 2; // 3段目の攻撃にのみ適用

        return isWarriorMode && isLastCombo;
    }

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
