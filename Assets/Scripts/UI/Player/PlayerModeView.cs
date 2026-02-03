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
        _raijinUI.gameObject.SetActive(mode == PlayerMode.Warrior);
        _toujinUI.gameObject.SetActive(mode == PlayerMode.Thunder);
    }

    [Header("Mode Images")]
    [SerializeField] private Image _raijinUI;
    [SerializeField] private Image _toujinUI;
   
}
