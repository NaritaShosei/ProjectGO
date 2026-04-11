using UnityEngine;

/// <summary>
/// ロックオン対象に実装するインターフェース。
/// </summary>
public interface ILockOnTarget
{
    /// <summary>
    /// ロックオンなどの中心のTransformを取得する
    /// </summary>
    public Transform GetTargetCenter();

    /// <summary>
    /// ロックオン可能か(非アクティブ状態でオフにしたい場合など)。
    /// </summary>
    bool IsLockable { get; }
}
