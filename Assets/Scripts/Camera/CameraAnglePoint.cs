using UnityEngine;

public class CameraAnglePoint : MonoBehaviour, ILockOnTarget
{
    /// <summary>
    /// ロックオン可能か(非アクティブ状態でオフにしたい場合など)。
    /// </summary>
    public bool IsLockable => false;

    /// <summary>
    /// カメラから見たこのAnglePointの位置
    /// </summary>
    public CameraAnglePointType AnglePoint => _anglePoint;

    /// <summary>
    /// ロックオンなどの中心のTransformを取得する
    /// </summary>
    public Transform GetTargetCenter()
    {
        return this.transform;
    }

    [SerializeField] private CameraAnglePointType _anglePoint;
}
