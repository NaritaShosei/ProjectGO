using System;
using UnityEngine;

/// <summary>
/// プレイヤーがダメージを受けた際のリアクション強度。
/// 指定しない既存の攻撃は Small として扱う。
/// </summary>
public enum DamageReactionType
{
    Small = 0,
    Medium = 1,
    Large = 2
}

public interface IHealth
{
    public event Action<float, float, float> OnHealthChanged;
    public void Healing(float amount);
    public void TakeDamage(float damage);
    public void TakeDamage(float damage, DamageReactionType reactionType);
}
