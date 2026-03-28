using UnityEngine;

[CreateAssetMenu(fileName = "StatsData", menuName = "GameData/StatsData")]
public class StatsData : ScriptableObject
{
    public float MaxHealth => _maxHealth;
    public float MaxThunderGauge => _maxThunderGauge;

    [SerializeField] private float _maxHealth = 100;

    [Header("雷ゲージ")]
    [Min(0f)]
    [SerializeField] private float _maxThunderGauge = 100;
}
