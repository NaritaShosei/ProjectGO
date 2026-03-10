using UnityEngine;

/// <summary>
/// Enemyの回転速度を制限するためのデータ
/// </summary>
[CreateAssetMenu(fileName = "TurnProfile", menuName = "GameData/Enemy/TurnProfile")]
public class TurnProfile : ScriptableObject
{
    // 最小回転速度（deg/sec）：角度差が maxAngle に近いほどこの値に近づく
    [Min(0f)]
    public float minTurnSpeed = 90f;

    // 最大回転速度（deg/sec）：角度差が小さいほどこの値に近づく
    [Min(0f)]
    public float maxTurnSpeed = 360f;

    // 回転速度補間の正規化基準角度（deg）
    // この角度差のときに minTurnSpeed を使用する
    [Min(1f)]
    public float maxAngle = 180f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // minTurnSpeed が maxTurnSpeed を超えると Lerp の結果が逆転する
        if (minTurnSpeed > maxTurnSpeed)
        {
            minTurnSpeed = maxTurnSpeed;
            Debug.LogWarning(
                $"[TurnProfile] minTurnSpeed が maxTurnSpeed ({maxTurnSpeed}) を超えています。maxTurnSpeed に補正しました。",
                this
            );
        }
    }
#endif
}
