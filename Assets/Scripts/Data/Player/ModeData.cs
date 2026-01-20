using UnityEngine;

[CreateAssetMenu(fileName = "ModeData", menuName = "GameData/ModeData")]

public class ModeData : ScriptableObject
{
    public float AttackMultiplier => _attackMultiplier;
    public float ArmorDamageMultiplier => _armorDamageMultiplier;
    public float FleshDamageMultiplier => _fleshDamageMultiplier;
    public float CriticalDamageMultiplier => _criticalDamageMultiplier;

    public float MoveSpeed => _moveSpeed;

    [Header("Attack Structure")]
    [SerializeField] private float _attackMultiplier = 1.0f;
    [SerializeField] private float _armorDamageMultiplier = 1.0f;
    [SerializeField] private float _fleshDamageMultiplier = 1.0f;
    [SerializeField] private float _criticalDamageMultiplier = 1.5f;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5.0f;
}
