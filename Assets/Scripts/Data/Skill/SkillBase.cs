using UnityEngine;

public class SkillBase : ScriptableObject, ISkill
{
    public int Priority => _priority;

    public virtual AttackContext Apply(AttackContext context)
    {
        return context;
    }

    public virtual bool CanApply(AttackContext context,AttackData data)
    {
        return true;
    }

    [SerializeField] private int _priority;
}
