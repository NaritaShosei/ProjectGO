using UnityEngine;

/// <summary>
/// Enemyの移動時の基準や制限を定めたもの
/// </summary>
[CreateAssetMenu(fileName = "DistanceProfile", menuName = "GameData/Enemy/DistanceProfile")]
public class DistanceProfile : ScriptableObject
{
    // プレイヤーを発見する距離
    public float DetectDistance = 10.0f;

    public float MinAttackDistance = 1.5f;
    public float MaxAttackDistance = 2.5f;

    public float DesiredDistance = 2.0f;
    public float DesiredTolerance = 0.5f;

    public float RoamRadius = 3.0f;

    public float SeparationRadius = 1.5f;
    public float SeparationStrength = 0.8f;

    public float WallDetectDistance = 1.0f;
    public float WallAvoidanceStrength = 0.8f;
}
