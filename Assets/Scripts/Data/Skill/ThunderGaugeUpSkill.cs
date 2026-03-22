using UnityEngine;

/// <summary>
/// 雷ゲージの上限を解放するスキル。
/// StaminaUpSkill と同じ構造。
/// </summary>
[CreateAssetMenu(fileName = "ThunderGaugeUpSkill", menuName = "GameData/Skill/ThunderGaugeUpSkill")]
public class ThunderGaugeUpSkill : SkillBase
{
    public override bool CanAcquire(int acquireCount)
        => acquireCount < _thunderGaugeUps.Length;

    public override void OnAcquire(IPlayerStats stats, int acquireCount)
    {
        if (acquireCount < 1 || acquireCount > _thunderGaugeUps.Length)
        {
            Debug.LogWarning($"ThunderGaugeUpSkill: acquireCount({acquireCount}) is out of range.");
            return;
        }
        stats.AddMaxThunderGauge(_thunderGaugeUps[acquireCount - 1]);
    }

    [Tooltip("獲得回数ごとの上限上昇値。例: [20, 20, 30] なら1回目+20、2回目+20、3回目+30")]
    [SerializeField] private float[] _thunderGaugeUps;
}
