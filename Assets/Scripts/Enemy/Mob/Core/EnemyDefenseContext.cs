using UnityEngine;

/// <summary>
/// Enemyのダメージ計算に使用する防御コンテキスト
/// DamageSystem.Calculate() に渡す
/// </summary>
public struct EnemyDefenseContext
{
    /// <summary>鎧 / 生身</summary>
    public EnemyDefenceType EnemyType;
    /// <summary>感電弱体化状態か</summary>
    public bool HasShockDebuff;
}

/// <summary>
/// Enemyの防御種別
/// </summary>
public enum EnemyDefenceType
{
    [InspectorName("生身")]
    Flesh,
    [InspectorName("鎧")]
    Armor,
}
