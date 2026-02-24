using UnityEngine;

[CreateAssetMenu(fileName = "DefenceUpSkill", menuName = "GameData/Skill/DefenceUpSkill")]
public class DefenceUpSkill : SkillBase
{
    public override bool CanAcquire(int acquireCount)
    {
        return acquireCount < _defenceUps.Length;
    }

    public override void OnAcquire(IPlayerStats stats, int acquireCount)
    {
        if (acquireCount < 1 || acquireCount > _defenceUps.Length)
        {
            Debug.LogWarning($"DefenceUpSkill: acquireCount({acquireCount}) is out of range.");
            return;
        }

        stats.AddDefensePower(_defenceUps[acquireCount - 1]);
        Debug.Log(stats.DefensePower);
    }
    [SerializeField] private float[] _defenceUps;
}
