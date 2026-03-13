public interface IItemPickupView
{
    /// <summary>
    /// 表示可能状態
    /// </summary>
    void ShowNear();

    /// <summary>
    /// 取得可能状態
    /// </summary>
    void ShowInteract();

    /// <summary>
    /// UI非表示
    /// </summary>
    void Hide();
}
