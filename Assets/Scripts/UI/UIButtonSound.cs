using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UIボタンの選択およびクリックに対応する効果音を再生する。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    /// <summary>
    /// キーボードやゲームパッドによってボタンが選択されたときにカーソル移動音を鳴らす。
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        PlayCursorMoveSound();
    }

    /// <summary>
    /// マウスカーソルがボタンに入るたびにカーソル移動音を鳴らす。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayCursorMoveSound();
    }

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlayConfirmSound);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(PlayConfirmSound);
        }
    }

    private void PlayCursorMoveSound()
    {
        if (_button.IsInteractable())
        {
            Sound.PlaySE(gameObject, SoundCueNames.UI.CursorMove, CueSheetType.UI);
        }
    }

    private void PlayConfirmSound()
    {
        Sound.PlaySE(gameObject, SoundCueNames.UI.Confirm, CueSheetType.UI);
    }
}
