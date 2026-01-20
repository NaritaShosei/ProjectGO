using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "GameData/PlayerData")]
public class PlayerData : ScriptableObject
{
    public StatsData Stats => _stats;
    public float AttackPower => _attackPower;
    public float BlockPower => _blockPower;
    public float CriticalRate => _criticalRate;
    public float HealAmount => _healAmount;
    public float StaminaRegenPerSecond => _staminaRegenPerSecond;
    public float DodgeStaminaCost => _dodgeStaminaCost;

    [SerializeField] private StatsData _stats;

    [SerializeField] private float _attackPower;
    [SerializeField] private float _blockPower;

    // クリティカル発生率（0.0〜1.0想定）
    [SerializeField] private float _criticalRate;

    // 回復量（1回あたり）
    [SerializeField] private float _healAmount;

    // スタミナ自然回復量（1秒あたり）
    [SerializeField] private float _staminaRegenPerSecond;

    // 回避時のスタミナ消費量
    [SerializeField] private float _dodgeStaminaCost;
}
