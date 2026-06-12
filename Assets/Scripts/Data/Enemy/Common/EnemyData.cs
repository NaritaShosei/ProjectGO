using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemyの基本パラメータをまとめたデータ
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "GameData/Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    // ステータス
    public float MaxHP => _maxHP;

    // 徘徊速度（RoamBehaviour）
    public float RoamSpeed => _roamSpeed;

    // 接近速度（ApproachBehaviour）
    public float ApproachSpeed => _approachSpeed;

    // 攻撃パターンリスト（射程・判定・ダメージ・クールダウンはパターン側に定義）
    public List<EnemyAttackPattern> AttackPatterns => _attackPatterns;

    // ノックバックレベル閾値
    public float KnockbackHitThreshold => _knockbackHitThreshold;
    public float KnockbackLargeThreshold => _knockbackLargeThreshold;

    // 停止後の硬直時間（秒）Hit / Small のみ使用
    public float KnockbackStunDuration => _knockbackStunDuration;

    // 水平方向の減速度（単位/秒²）。飛距離の目安 ≈ Power² / (2 × 減速度)
    public float KnockbackDeceleration => _knockbackDeceleration;

    // 前衛選出の優先度。値が高いほど前衛に選ばれやすい
    public float CombatPower => _combatPower;

    // Bark継続時間
    public float BarkDuration => _barkDuration;

    // スロット待ち時にBarkを選ぶ確率（0〜1）
    public float BarkChance => _barkChance;

    // 死亡時にヒールアイテムをドロップする確率（0〜1）
    public float HealDropChance => _healDropChance;

    // 経験値ドロップ量
    public int ExpDropAmount => Mathf.Max(0, _expDropAmount);

    // Idle継続時間（秒）
    public float IdleDuration => _idleDuration;

    // 死亡ノックバックの水平速度
    public float DeathKnockbackPower => _deathKnockbackPower;

    // 死亡ノックバックの初期垂直速度
    public float DeathKnockbackUpward => _deathKnockbackUpward;


    [Header("Status")]
    [SerializeField, Tooltip("最大HP")] private float _maxHP = 100f;

    [Header("Movement")]
    [Min(0f)]
    [SerializeField, Tooltip("徘徊時のSpeed")] private float _roamSpeed = 1f;
    [Min(0f)]
    [SerializeField] private float _approachSpeed = 3f;

    [Header("Attack")]
    [SerializeField, Tooltip("Attack")] private List<EnemyAttackPattern> _attackPatterns = new();

    [Header("Knockback")]
    // Power がこの値以下なら Hit（level 0）
    [SerializeField] private float _knockbackHitThreshold = 5f;
    // Power がこの値以上なら Large（level 2）
    [SerializeField] private float _knockbackLargeThreshold = 20f;
    [SerializeField] private float _knockbackStunDuration = 0.1f;
    [Tooltip("0以下を設定すると水平速度が収束せず死亡ノックバックが終了しなくなるため 0.01 以上を強制する")]
    [Min(0.01f)]
    [SerializeField] private float _knockbackDeceleration = 20f;

    [Header("Formation")]
    [SerializeField] private float _combatPower = 1f;

    [Header("Bark")]
    [SerializeField] private float _barkDuration = 2.0f;

    [Range(0f, 1f)]
    [SerializeField] private float _barkChance = 0.5f;

    [Header("Idle")]
    [Min(0f)]
    [SerializeField] private float _idleDuration = 1.0f;

    [Header("Death")]
    [Min(0f)]
    [SerializeField] private float _deathKnockbackPower = 8f;
    [Min(0f)]
    [SerializeField] private float _deathKnockbackUpward = 2f;

    [Header("Drop")]
    [Range(0f, 1f)]
    [SerializeField] private float _healDropChance = 0.3f;
    [Min(0)]
    [SerializeField] private int _expDropAmount = 10;
}
