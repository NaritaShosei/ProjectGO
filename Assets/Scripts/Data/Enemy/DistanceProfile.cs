using UnityEngine;

/// <summary>
/// Enemyの移動時の基準や制限を定めたもの
/// </summary>
[CreateAssetMenu(fileName = "DistanceProfile", menuName = "GameData/Enemy/DistanceProfile")]
public class DistanceProfile : ScriptableObject
{
    // プレイヤーを発見する距離
    [Min(0f)]
    public float DetectDistance = 10.0f;

    // 攻撃を開始する最短距離
    [Min(0f)]
    public float MinAttackDistance = 1.5f;

    // 攻撃を継続できる最長距離
    [Min(0f)]
    public float MaxAttackDistance = 2.5f;

    // 移動目標とするプレイヤーとの理想距離
    [Min(0f)]
    public float DesiredDistance = 2.0f;

    // 理想距離の許容誤差（この範囲内なら停止とみなす）
    [Min(0f)]
    public float DesiredTolerance = 0.5f;

    // 徘徊時の移動半径
    [Min(0f)]
    public float RoamRadius = 3.0f;

    // 他の敵との分離を開始する距離
    [Min(0f)]
    public float SeparationRadius = 1.5f;

    // 分離力の強さ
    [Min(0f)]
    public float SeparationStrength = 0.8f;

    // 壁を検知する距離
    [Min(0f)]
    public float WallDetectDistance = 1.0f;

    // 壁回避力の強さ
    [Min(0f)]
    public float WallAvoidanceStrength = 0.8f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // DetectDistance は MinAttackDistance 以上でなければ索敵後に即攻撃になる
        if (DetectDistance < MinAttackDistance)
        {
            DetectDistance = MinAttackDistance;
            Debug.LogWarning(
                $"[DistanceProfile] DetectDistance を MinAttackDistance ({MinAttackDistance}) に補正しました。",
                this
            );
        }

        // MaxAttackDistance は MinAttackDistance 以上でなければ攻撃距離が逆転する
        if (MaxAttackDistance < MinAttackDistance)
        {
            MaxAttackDistance = MinAttackDistance;
            Debug.LogWarning(
                $"[DistanceProfile] MaxAttackDistance を MinAttackDistance ({MinAttackDistance}) に補正しました。",
                this
            );
        }

        // DesiredDistance は MinAttackDistance 以上かつ MaxAttackDistance 以下が自然
        DesiredDistance = Mathf.Clamp(DesiredDistance, MinAttackDistance, MaxAttackDistance);
    }
#endif
}
