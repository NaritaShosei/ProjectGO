using UnityEngine;

[CreateAssetMenu(fileName = "TestSkill", menuName = "GameData/Skill/TestSkill")]
public class TestSkill : SkillBase
{
    public override AttackContext Apply(AttackContext context)
    {
        context.OnHit += () => GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
        return context;
    }

    public override bool CanApply(AttackContext context, AttackData data)
    {
        return data.ComboIndex == 2;
    }
}
