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

    [SerializeField] private ButtonConfirmSoundType _buttonConfirmSoundType = ButtonConfirmSoundType.Confirm;

    private Button _button;

    private enum ButtonConfirmSoundType
    {
        Confirm = 0,
        SkillSelectAppear = 1
    }

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
        switch (_buttonConfirmSoundType)
        {
            case ButtonConfirmSoundType.Confirm:
                Sound.PlaySE(gameObject, SoundCueNames.UI.Confirm, CueSheetType.UI);
                break;
            case ButtonConfirmSoundType.SkillSelectAppear:
                Sound.PlaySE(gameObject, SoundCueNames.UI.SkillSelectAppear, CueSheetType.UI);
                break;
        }
    }
}
