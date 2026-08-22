using System;
using UnityEngine;

/// <summary>
/// UIなどの外部購読者がアーマーのHP変化・破壊を受け取るためのインターフェース
/// </summary>
public interface IArmorHealth
{
    /// <summary>現在HP</summary>
    float CurrentHealth { get; }

    /// <summary>最大HP</summary>
    float MaxHealth { get; }

    /// <summary>HP変化時に発火するイベント（current, max）</summary>
    event Action<float, float> OnHealthChanged;

    /// <summary>アーマーが破壊された際に発火するイベント</summary>
    event Action OnBroken;

    /// <summary>アーマーのワールド座標（UIのアンカー等に使用）</summary>
    Transform GetTargetCenter();
}
