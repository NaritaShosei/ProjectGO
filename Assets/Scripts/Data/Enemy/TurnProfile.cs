using UnityEngine;

/// <summary>
/// Enemyの回転速度を制限するためのデータ
/// </summary>
[CreateAssetMenu(menuName = "Data/Enemy/TurnProfile")]
public class TurnProfile : ScriptableObject
{
    public float minTurnSpeed = 90f;   // deg/sec
    public float maxTurnSpeed = 360f;  // deg/sec
    public float maxAngle = 180f;      // 正規化用
}
