using UnityEngine;

public interface IItemInteractHandler
{
    /// <summary>
    /// プレイヤーがインタラクトしたとき呼ぶ
    /// </summary>
    void OnPlayerInteract(GameObject interactor);

    /// <summary>
    /// プレイヤーの探索範囲内に入ったアイテムを登録する
    /// </summary>
    void SetNearTarget(IInteractable interactable);

    /// <summary>
    /// 範囲外に出たとき登録を解除する
    /// </summary>
    void ClearNearTarget(IInteractable interactable);
}
