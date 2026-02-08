using UnityEngine;

public interface ISkill
{
    /// <summary> 発動優先度 </summary>
    public int Priority { get; }
    /// <summary> 発動タイミング </summary>
    public SkillTiming Timing { get; }

    /// <summary> 発動条件 </summary>
    public bool CanApply(AttackContext context, AttackData data);
    /// <summary> スキルを発動する,攻撃力の変化などはcontextの内部のパラメーターに行う </summary>
    public void Apply(ref AttackContext context);

    /// <summary> スキル獲得可能かどうか </summary>
    public bool CanAcquire(int acquireCount);
    /// <summary> スキル獲得時に呼ばれる(acquireCountは累計獲得回数) </summary>
    public void OnAcquire(IAttackStats stats, int acquireCount);
}

public enum SkillTiming
{
    [InspectorName("攻撃時")]
    OnAttack,      // 攻撃時スキル
    [InspectorName("獲得時")]
    OnAcquire,     // 獲得時に一度だけ適用
    [InspectorName("常時")]
    Passive        // 常時効果
}
