using UnityEngine;

public class SkillBase : ScriptableObject, ISkill
{
    public int ID => _id;
    public int Priority => _priority;
    public string Name => _name;
    public string Explanation => _explanation;
    public Sprite Icon => _icon;
    public SkillTiming Timing => _timing;

    public virtual void Apply(ref AttackContext context)
    {
    }

    public virtual bool CanApply(AttackContext context, AttackData data)
    {
        return true;
    }

    public virtual void OnAcquire(IPlayerStats stats, int acquireCount)
    {
    }

    public virtual bool CanAcquire(int acquireCount)
    {
        return false;
    }

    [SerializeField] private int _id;               // 検索用ID
    [SerializeField] private int _priority;         // スキル発動順優先度
    [SerializeField] private string _name;          // スキルの名前
    [SerializeField] private string _explanation;   // スキルの説明
    [SerializeField] private Sprite _icon;          // スキルのアイコン画像
    [SerializeField] private SkillTiming _timing = SkillTiming.OnAttack; // スキルの発動タイミング
}
