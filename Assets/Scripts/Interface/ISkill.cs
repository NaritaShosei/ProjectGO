public interface ISkill
{
    /// <summary>
    /// 発動優先度
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// 発動条件
    /// </summary>
    public bool CanApply(AttackContext context);

    /// <summary>
    /// スキルを発動する
    /// </summary>
    public AttackContext Apply(AttackContext context);
}
