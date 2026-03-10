using UnityEngine;

/// <summary>
/// Enemyの移動時の基準や制限を定めたもの
/// </summary>
[CreateAssetMenu(fileName = "DistanceProfile", menuName = "GameData/Enemy/DistanceProfile")]
public class DistanceProfile : ScriptableObject
{
    // プレイヤーを発見する距離
    public float DetectDistance => _detectDistance;

    // 攻撃を開始する最短距離
    public float MinAttackDistance => _minAttackDistance;

    // 攻撃を継続できる最長距離
    public float MaxAttackDistance => _maxAttackDistance;

    // 移動目標とするプレイヤーとの理想距離
    public float DesiredDistance => _desiredDistance;

    // 理想距離の許容誤差（この範囲内なら停止とみなす）
    public float DesiredTolerance => _desiredTolerance;

    // 徘徊時の移動半径
    public float RoamRadius => _roamRadius;

    // 他の敵との分離を開始する距離
    public float SeparationRadius => _separationRadius;

    // 分離力の強さ
    public float SeparationStrength => _separationStrength;

    // 壁を検知する距離
    public float WallDetectDistance => _wallDetectDistance;

    // 壁回避力の強さ
    public float WallAvoidanceStrength => _wallAvoidanceStrength;


    [Min(0f)]
    [SerializeField] private float _detectDistance = 10.0f;

    [Min(0f)]
    [SerializeField] private float _minAttackDistance = 1.5f;

    [Min(0f)]
    [SerializeField] private float _maxAttackDistance = 2.5f;

    [Min(0f)]
    [SerializeField] private float _desiredDistance = 2.0f;

    [Min(0f)]
    [SerializeField] private float _desiredTolerance = 0.5f;

    [Min(0f)]
    [SerializeField] private float _roamRadius = 3.0f;

    [Min(0f)]
    [SerializeField] private float _separationRadius = 1.5f;

    [Min(0f)]
    [SerializeField] private float _separationStrength = 0.8f;

    [Min(0f)]
    [SerializeField] private float _wallDetectDistance = 1.0f;

    [Min(0f)]
    [SerializeField] private float _wallAvoidanceStrength = 0.8f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // DetectDistance は MinAttackDistance 以上でなければ索敵後に即攻撃になる
        if (_detectDistance < _minAttackDistance)
        {
            _detectDistance = _minAttackDistance;
            Debug.LogWarning(
                $"[DistanceProfile] DetectDistance を MinAttackDistance ({_minAttackDistance}) に補正しました。",
                this
            );
        }

        // MaxAttackDistance は MinAttackDistance 以上でなければ攻撃距離が逆転する
        if (_maxAttackDistance < _minAttackDistance)
        {
            _maxAttackDistance = _minAttackDistance;
            Debug.LogWarning(
                $"[DistanceProfile] MaxAttackDistance を MinAttackDistance ({_minAttackDistance}) に補正しました。",
                this
            );
        }

        // DesiredDistance は MinAttackDistance 以上かつ MaxAttackDistance 以下が自然
        _desiredDistance = Mathf.Clamp(_desiredDistance, _minAttackDistance, _maxAttackDistance);
    }
#endif
}
