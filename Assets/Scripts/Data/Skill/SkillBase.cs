using UnityEngine;

public class SkillBase : ScriptableObject, ISkill
{
    public int ID => _id;
    public int Priority => _priority;

    public virtual DamageContext Apply(ref AttackContext context)
    {
        return new DamageContext()
        {
            Damage = context.Damage,
            PlayerMode = context.PlayerMode,
        };
    }

    public virtual bool CanApply(AttackContext context, AttackData data)
    {
        return true;
    }

    [SerializeField] private int _id;
    [SerializeField] private int _priority;
}
