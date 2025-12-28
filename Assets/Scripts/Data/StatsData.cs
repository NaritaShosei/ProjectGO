using UnityEngine;

[CreateAssetMenu(fileName = "StatsData", menuName = "GameData/StatsData")]

public class StatsData : ScriptableObject
{
    public float MaxHealth => _maxHealth;
    public float MaxStamina => _maxStamina;

    [SerializeField] private float _maxHealth;
    [SerializeField] private float _maxStamina;
}