using UnityEngine;

[CreateAssetMenu(fileName = "StaminaUpSkill", menuName = "GameData/Skill/StaminaUpSkill")]
public class TestStaminaUpSkill : SkillBase
{
    public override bool CanAcquire(int acquireCount)
    {
        return acquireCount < _staminaUps.Length;
    }

    public override void OnAcquire(IPlayerStats stats, int acquireCount)
    {
        if (acquireCount < 1 || acquireCount > _staminaUps.Length)
        {
            Debug.LogWarning($"StaminaUpSkill: acquireCount({acquireCount}) is out of range.");
            return;
        }

        stats.AddMaxStamina(_staminaUps[acquireCount - 1]);
    }

    [SerializeField] private float[] _staminaUps;
}
