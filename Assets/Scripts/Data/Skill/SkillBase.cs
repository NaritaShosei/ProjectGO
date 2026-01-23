using UnityEngine;

public class SkillBase : ScriptableObject, ISkill
{
    public int ID => _id;
    public int Priority => _priority;
    public string Name => _name;
    public string Explanation => _explanation;

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

    [SerializeField] private int _id;               // 検索用ID
    [SerializeField] private int _priority;         // スキル発動順優先度
    [SerializeField] private string _name;          // スキルの名前
    [SerializeField] private string _explanation;   // スキルの説明
}
