using UnityEngine;

[CreateAssetMenu(fileName = "GroundCrushSkill", menuName = "GameData/Skill/GroundCrushSkill")]

public class GroundCrush : SkillBase
{
    public override void Apply(ref AttackContext context)
    {
        _damage = context.EvolutionGroundCrush.Damage;

        Collider[] enemies = Physics.OverlapSphere(_attackCenter, _attackRadius);

        if (enemies.Length > 0)
        {
            Debug.Log($"{enemies.Length}体の敵に{_damage}ダメージ");
        }
        else return;
    }

    public override bool CanApply(AttackContext context, AttackData data)
    {
        bool isWorrior = context.PlayerMode == _isPlayerMode;

        bool isTargetAttackType = data.AttackType == _attackType;

        bool isComboCount = data.ComboIndex >= _getComboCount;

        bool isLastCombo = data.NextComboAttackId == -1;

        return isWorrior
            && isTargetAttackType
            && isComboCount
            && isLastCombo;
    }

    [SerializeField] private int _getComboCount = 1;                          //コンボの何段目に実行するか
    [SerializeField] private float _attackRadius = 1;                         //攻撃範囲
    [SerializeField] private float _damage;                                   //与えるダメージ
    [SerializeField] private Vector3 _attackCenter = new Vector3(1, 1, 1);    //攻撃位置
    [SerializeField] private AttackType _attackType;                          //攻撃タイプ
    [SerializeField] private PlayerMode _isPlayerMode = PlayerMode.Warrior;   //プレイヤーが闘神モードかどうか
}

public struct EvolutionGroundCrush
{
    public float Damage;
}
