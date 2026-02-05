using UnityEngine;
using UnityEngine.UI;
using Interface;

/// <summary>
/// 現在のプレイヤーモードのUI表示
/// </summary>
public class PlayerModeView : MonoBehaviour,IPlayerModeView
{
    /// <summary>
    /// UIをモードによって切り替える
    /// </summary>
    /// <param name="mode"></param>
    public void SetMode(PlayerMode mode)
    {
        _warriorUI.gameObject.SetActive(mode == PlayerMode.Warrior);
        _thunderUI.gameObject.SetActive(mode == PlayerMode.Thunder);
    }

    [Header("Mode Images")]
    [SerializeField] private Image _warriorUI;//闘神UI
    [SerializeField] private Image _thunderUI;//雷神UI
   
}
