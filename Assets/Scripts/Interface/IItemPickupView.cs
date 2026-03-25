using UnityEngine;

public interface IItemPickupView
{
    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="target"></param>
    void Initialize(Transform target);

    /// <summary>
    /// UIの状態変化
    /// </summary>
    /// <param name="state"></param>
    void SetState(ItemPickupViewState state);
}
