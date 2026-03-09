using UnityEngine;

[CreateAssetMenu(menuName = "Data/Enemy/EnemyAttackPattern")]
public sealed class EnemyAttackPattern : ScriptableObject
{
    public string PatternName;

    [Header("Slot")]
    public int SlotCost = 1;

    [Header("Timing")]
    public float WindUp;
    public float Duration;
    public float Cooldown;

    [Header("Hit")]
    public int MaxHitCount = 1; // Bossは複数Hit可
    public float HitInterval = 0.2f;

    [Header("Knockback")]
    public float KnockbackPower;

    [Header("Damage")]
    public int BaseDamage;
}
