using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戦闘を止めずに、基本操作の達成状況を画面端へ表示するチェックリスト。
/// 位置と大きさはPrefab側で調整し、実行中には変更しない。
/// </summary>
public sealed class TutorialChecklistView : MonoBehaviour
{
    private const string MODE_CHANGE_INPUT_TEXT = "\n\n<size=32><color=#FFD447><b>LB　モードチェンジ</b></color></size>";

    public void ShowBasicOperations(string title)
    {
        if (_titleText != null)
            _titleText.text = title;

        SetGroupVisible(true);
        SetModeChangeVisible(false);
        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    public void ShowModeChange(string title, string description)
    {
        if (_titleText != null)
            _titleText.text = title;
        if (_modeChangeDescriptionText != null)
            _modeChangeDescriptionText.text = $"{description}{MODE_CHANGE_INPUT_TEXT}";

        SetGroupVisible(false);
        SetModeChangeVisible(true);
        SetToggleValue(_modeChangeToggle, false);
        SetVisible(true);
    }

    public void SetBasicOperationProgress(
        bool moved,
        bool cameraMoved,
        bool lockedOn,
        bool dodged,
        bool attacked)
    {
        SetToggleValue(_moveToggle, moved);
        SetToggleValue(_cameraMoveToggle, cameraMoved);
        SetToggleValue(_lockOnToggle, lockedOn);
        SetToggleValue(_dodgeToggle, dodged);
        SetToggleValue(_attackToggle, attacked);
    }

    public void CompleteModeChange()
    {
        SetToggleValue(_modeChangeToggle, true);
    }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private GameObject _basicOperationGroup;
    [SerializeField] private GameObject _modeChangeGroup;
    [SerializeField] private TMP_Text _modeChangeDescriptionText;
    [SerializeField] private Toggle _moveToggle;
    [SerializeField] private Toggle _cameraMoveToggle;
    [SerializeField] private Toggle _lockOnToggle;
    [SerializeField] private Toggle _dodgeToggle;
    [SerializeField] private Toggle _attackToggle;
    [SerializeField] private Toggle _modeChangeToggle;

    private void Awake() => SetVisible(false);

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        // リアルタイム表示が戦闘入力や他のUIを遮らないようRaycastは常に無効にする。
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void SetGroupVisible(bool visible)
    {
        if (_basicOperationGroup != null)
            _basicOperationGroup.SetActive(visible);
    }

    private void SetModeChangeVisible(bool visible)
    {
        if (_modeChangeGroup != null)
            _modeChangeGroup.SetActive(visible);
    }

    private static void SetToggleValue(Toggle toggle, bool value)
    {
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(value);
    }
}
