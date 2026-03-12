using UnityEngine;

[CreateAssetMenu(fileName = "GroundCrushSkill", menuName = "GameData/Skill/GroundCrushSkill")]

public class GroundCrush : SkillBase
{
    public override void Apply(ref AttackContext context)
    {
        context.EvolutionGroundCrush.Determine = _determine;
    }

    public override bool CanApply(AttackContext context, AttackData data)
    {
        bool isWorrior = context.PlayerMode == _isPlayerMode;

        bool isComboCount = data.ComboIndex >= _getComboCount;

        return isWorrior
            && isComboCount;
    }

    [SerializeField] private int _getComboCount = 1;
    [SerializeField] private PlayerMode _isPlayerMode = PlayerMode.Warrior;
    [SerializeField] private GameObject _determine;
}

public struct EvolutionGroundCrush
{
    public GameObject Determine;
}
