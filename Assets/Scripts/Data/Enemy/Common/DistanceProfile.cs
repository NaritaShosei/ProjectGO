using UnityEngine;

/// <summary>
/// Enemyの移動・徘徊・分離・壁回避に関する距離パラメータをまとめたデータ
/// </summary>
[CreateAssetMenu(fileName = "DistanceProfile", menuName = "GameData/Enemy/DistanceProfile")]
public class DistanceProfile : ScriptableObject
{
    // 徘徊時の移動半径
    public float RoamRadius => _roamRadius;

    // プレイヤーから離れられる最大距離（これ以上遠い場合はプレイヤーへ向かう）
    public float MaxRoamDistance => _maxRoamDistance;

    // Attacker でない敵がプレイヤーに近づける最小距離
    public float MinNonAttackerDistance => _minNonAttackerDistance;

    // Attacker がプレイヤーに近づける最小距離（Roam時の目標選定に使用）
    public float MinAttackerRoamDistance => _minAttackerRoamDistance;

    // 他の敵との分離を開始する距離
    public float SeparationRadius => _separationRadius;

    // 分離力の強さ
    public float SeparationStrength => _separationStrength;

    // 壁を検知する距離
    public float WallDetectDistance => _wallDetectDistance;

    // 壁回避力の強さ
    public float WallAvoidanceStrength => _wallAvoidanceStrength;


    // Moveを終了してAttackに譲る距離（AttackRangeに対する割合）
    public float MoveApproachRatio => _moveApproachRatio;

    // 背後攻撃を抑制するプレイヤー正面からの角度閾値（度）
    public float BackAttackAngle => _backAttackAngle;

    // 背後にいるときに攻撃をキャンセルする確率（0〜1）
    public float BackAttackSuppressChance => _backAttackSuppressChance;

    [Header("Movement")]
    [Tooltip("MoveをやめてAttackに譲る距離（AttackRangeに対する割合。0.8 = 80%地点で停止）")]
    [Range(0f, 1f)]
    [SerializeField] private float _moveApproachRatio = 0.8f;

    [Header("Back Attack")]
    [Tooltip("背後と判定するプレイヤー正面からの角度（90° = 正面±90°以外は背後）")]
    [Range(0f, 180f)]
    [SerializeField] private float _backAttackAngle = 90f;

    [Tooltip("背後にいるときに攻撃をキャンセルする確率（0.8 = 80%キャンセル）")]
    [Range(0f, 1f)]
    [SerializeField] private float _backAttackSuppressChance = 0.8f;

    [Header("Roam")]
    [Min(0f)]
    [SerializeField] private float _roamRadius = 3.0f;

    [Min(0f)]
    [SerializeField] private float _maxRoamDistance = 6.0f;

    [Min(0f)]
    [SerializeField] private float _minNonAttackerDistance = 3.0f;

    [Tooltip("Attackerがプレイヤーに近づける最小距離。Roam目標選定時に適用する")]
    [Min(0f)]
    [SerializeField] private float _minAttackerRoamDistance = 2.5f;

    [Header("Separation")]
    [Min(0f)]
    [SerializeField] private float _separationRadius = 1.5f;

    [Min(0f)]
    [SerializeField] private float _separationStrength = 0.8f;

    [Header("Wall Avoidance")]
    [Min(0f)]
    [SerializeField] private float _wallDetectDistance = 1.0f;

    [Min(0f)]
    [SerializeField] private float _wallAvoidanceStrength = 0.8f;
}
