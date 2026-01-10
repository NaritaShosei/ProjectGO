using System;
using UnityEngine;

/// <summary>
/// 回避攻撃の種類
/// </summary>
public enum DodgeAttackType
{
    [InspectorName("弱攻撃")]
    LightAttack,
    [InspectorName("強攻撃")]
    HeavyAttack,
    [InspectorName("回避攻撃なし")]
    None
}

/// <summary>
/// 回避攻撃の設定を管理
/// </summary>
[CreateAssetMenu(fileName = "DodgeAttackConfig", menuName = "GameData/DodgeAttackConfig")]
public class DodgeAttackConfig : ScriptableObject
{
    public DodgeAttackType DodgeAttackType => _dodgeAttackType;

    /// <summary>
    /// 回避攻撃が有効かどうか
    /// </summary>
    public bool IsEnabled => DodgeAttackType != DodgeAttackType.None;

    /// <summary>
    /// 回避攻撃の入力データを生成
    /// </summary>
    public AttackInput CreateAttackInput()
    {
        return new AttackInput
        {
            AttackType = AttackType.DodgeAttack
        };
    }

    [Header("回避攻撃の種類")]
    [Tooltip("回避中に発動する攻撃のタイプ")]
    [SerializeField] private DodgeAttackType _dodgeAttackType = DodgeAttackType.HeavyAttack;
}
