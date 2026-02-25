using UnityEngine;

[CreateAssetMenu(fileName = "HpUpSkill", menuName = "GameData/Skill/HpUpSkill")]
public class HpUpSkill : SkillBase
{
    public override bool CanAcquire(int acquireCount)
    {
        return acquireCount < _hpUps.Length;
    } 

    public override void OnAcquire(IPlayerStats stats, int acquireCount)
    {
        if (acquireCount < 1 || acquireCount > _hpUps.Length)
        {
            Debug.LogWarning($"HPUpSkill: acquireCount({acquireCount}) is out of range.");
            return;
        }

        stats.AddMaxHealth(_hpUps[acquireCount - 1]);
        Debug.Log(stats.MaxHealth);
    }

    [SerializeField] private float[] _hpUps;
}
