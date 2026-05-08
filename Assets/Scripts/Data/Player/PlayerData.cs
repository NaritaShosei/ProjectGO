using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "GameData/PlayerData")]
public class PlayerData : ScriptableObject
{
    public StatsData Stats => _stats;
    public float AttackPower => _attackPower;
    public float DefensePower => _defensePower;
    public float CriticalRate => _criticalRate;

    /// <summary> 雷神モード中の毎秒消費量。デフォルトで3秒で空になる </summary>
    public float ThunderDrainPerSecond => _thunderDrainPerSecond;
    /// <summary> 闘神モード中の毎秒回復量。デフォルトで3秒で全回復 </summary>
    public float ThunderRecoverPerSecond => _thunderRecoverPerSecond;

    [SerializeField] private StatsData _stats;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _defensePower;
    [SerializeField, Range(0, 1)] private float _criticalRate = 0.5f;

    [Header("雷ゲージ速度")]
    [Min(0f)]
    [SerializeField] private float _thunderDrainPerSecond = 100f / 3f;
    [Min(0f)]
    [SerializeField] private float _thunderRecoverPerSecond = 100f / 3f;
}
