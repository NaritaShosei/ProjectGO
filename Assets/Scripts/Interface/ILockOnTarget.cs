using UnityEngine;

/// <summary>
/// ロックオン対象に実装するインターフェース。
/// </summary>
public interface ILockOnTarget
{
    /// <summary>
    /// ロックオンポイント（ロックオンの中心点）。
    /// </summary>
    Transform LockOnPoint { get; }

    /// <summary>
    /// ロックオン可能か(非アクティブ状態でオフにしたい場合など)。
    /// </summary>
    bool IsLockable { get; }
}