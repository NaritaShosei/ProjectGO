public interface ISkill
{
    /// <summary>
    /// 発動優先度
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// 発動条件
    /// </summary>
    public bool CanApply(AttackContext context,AttackData data);

    /// <summary>
    /// スキルを発動する,攻撃力の変化などはcontextの内部のパラメーターに行う
    /// </summary>
    public void Apply(ref AttackContext context);
}
