using UnityEngine;

/// <summary>
/// Enemyの回転速度を制限するためのデータ
/// </summary>
[CreateAssetMenu(fileName = "TurnProfile", menuName = "GameData/Enemy/TurnProfile")]
public class TurnProfile : ScriptableObject
{
    [Min(0f)]
    [SerializeField] private float _minTurnSpeed = 90f;

    [Min(0f)]
    [SerializeField] private float _maxTurnSpeed = 360f;

    [Min(1f)]
    [SerializeField] private float _maxAngle = 180f;

    // 最小回転速度（deg/sec）：角度差が maxAngle に近いほどこの値に近づく
    public float MinTurnSpeed => _minTurnSpeed;

    // 最大回転速度（deg/sec）：角度差が小さいほどこの値に近づく
    public float MaxTurnSpeed => _maxTurnSpeed;

    // 回転速度補間の正規化基準角度（deg）
    public float MaxAngle => _maxAngle;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // minTurnSpeed が maxTurnSpeed を超えると Lerp の結果が逆転する
        if (_minTurnSpeed > _maxTurnSpeed)
        {
            _minTurnSpeed = _maxTurnSpeed;
            Debug.LogWarning(
                $"[TurnProfile] minTurnSpeed が maxTurnSpeed ({_maxTurnSpeed}) を超えています。maxTurnSpeed に補正しました。",
                this
            );
        }
    }
#endif
}
