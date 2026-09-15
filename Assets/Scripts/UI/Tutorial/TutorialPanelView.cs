using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// チュートリアルの1ページを、文字や背景を含む1枚のSpriteで表示する。
/// 表示内容やページ送りの判断は State 側が担当する。
/// </summary>
public sealed class TutorialPanelView : MonoBehaviour
{
    public event Action OnConfirmRequested;

    public void Show(Sprite sprite, bool modal = true)
    {
        if (_illustration != null)
        {
            _illustration.sprite = sprite;
            _illustration.preserveAspect = true;
            _illustration.enabled = sprite != null;
        }

        if (sprite == null)
            Debug.LogWarning("[TutorialPanelView] ページSpriteが未設定です。", this);

        ApplyPresentation(modal);
        SetVisible(true, modal);
        _shownFrame = Time.frameCount;
    }

    public void Hide() => SetVisible(false, false);

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _backdrop;
    [SerializeField, Tooltip("ページ全体のSpriteを表示するImage。位置と大きさはPrefabで調整します。")]
    private Image _illustration;
    private PlayerInput _input;
    private int _shownFrame;

    private void Awake()
    {
        _input = new PlayerInput();
        SetVisible(false, false);
    }

    // EventSystemの処理後に進め、同じ決定入力が遷移先のスキル選択へ流れるのを防ぐ。
    private void LateUpdate()
    {
        if (_input != null && _input.UI.Submit.enabled &&
            Time.frameCount > _shownFrame && _input.UI.Submit.WasPressedThisFrame())
            OnConfirmRequested?.Invoke();
    }

    private void OnDisable() => _input?.UI.Submit.Disable();

    private void OnDestroy()
    {
        if (_input == null)
            return;
        _input.Disable();
        _input.Dispose();
    }

    /// <summary>
    /// パネルの位置と大きさはPrefabで調整した値を維持し、用途に応じた部品だけを切り替える。
    /// </summary>
    private void ApplyPresentation(bool modal)
    {
        if (_backdrop != null)
            _backdrop.SetActive(modal);
    }

    private void SetVisible(bool visible, bool blocksInput)
    {
        if (_input != null)
        {
            if (visible && blocksInput)
                _input.UI.Submit.Enable();
            else
                _input.UI.Submit.Disable();
        }

        if (_canvasGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible && blocksInput;
        _canvasGroup.blocksRaycasts = visible && blocksInput;
    }
}
