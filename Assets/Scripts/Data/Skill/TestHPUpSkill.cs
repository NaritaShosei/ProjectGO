using UnityEngine;

[CreateAssetMenu(fileName = "HpUpSkill", menuName = "GameData/Skill/HpUpSkill")]
public class TestHPUpSkill : SkillBase
{
    public override bool CanAcquire(int acquireCount)
    {
        return acquireCount < _hpUps.Length;
    } 

    public override void OnAcquire(IPlayerStats stats, int acquireCount)
    {
        if (acquireCount < 1 || acquireCount > _hpUps.Length)
        {
            return;
        }

        stats.AddMaxHealth(_hpUps[acquireCount - 1]);
    }

    [SerializeField] private float[] _hpUps;
}
