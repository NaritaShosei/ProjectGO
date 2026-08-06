using UniRx;
using UnityEngine;

/// <summary> 画面内で動きを作る対象のインターフェース </summary>
public interface IMoveTarget
{
    /// <summary> 歩行速度 </summary>
    public float WalkSpeed { get; }

    /// <summary> 現在座標 </summary>
    public IReadOnlyReactiveProperty<Vector3> Position { get; }

    /// <summary> 回転情報 </summary>
    public IReadOnlyReactiveProperty<Quaternion> Rotation { get; }

    /// <summary> 移動速度 </summary>
    public IReadOnlyReactiveProperty<Vector3> Velocity { get; }

    /// <summary> 座標を設定する </summary>
    /// <param name="position"> 新しい座標 </param>
    public void SetPosition(Vector3 position);

    /// <summary> 回転を設定する </summary>
    /// <param name="rotation"> 新しい回転 </param>
    public void SetRotation(Quaternion rotation);

    /// <summary> 移動速度を設定する </summary>
    /// <param name="velocity"> 移動速度 </param>
    public void SetVelocity(Vector3 velocity);
}
