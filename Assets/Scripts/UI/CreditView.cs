using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CreditView : MonoBehaviour
{
    public event Action OnBackButtonClicked;

    public void Show()
    {
        gameObject.SetActive(true);

        ShowDevelopers();

        if (EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(
            _developersButton.gameObject);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    [Header("ボタン")]
    [SerializeField]
    private Button _developersButton;

    [SerializeField]
    private Button _assetCreditsButton;

    [SerializeField]
    private Button _backButton;

    [Header("クレジット画像")]
    [SerializeField]
    private Image _creditImage;

    [SerializeField]
    private Sprite _developersImage;

    [SerializeField]
    private Sprite _assetCreditsImage;

    private void Awake()
    {
        _developersButton.onClick.AddListener(ShowDevelopers);
        _assetCreditsButton.onClick.AddListener(ShowAssetCredits);
        _backButton.onClick.AddListener(HandleBackButtonClicked);

        SetupNavigation();
    }

    private void OnDestroy()
    {
        _developersButton.onClick.RemoveListener(ShowDevelopers);
        _assetCreditsButton.onClick.RemoveListener(ShowAssetCredits);
        _backButton.onClick.RemoveListener(HandleBackButtonClicked);
    }

    private void ShowDevelopers()
    {
        _creditImage.sprite = _developersImage;
    }

    private void ShowAssetCredits()
    {
        _creditImage.sprite = _assetCreditsImage;
    }

    private void HandleBackButtonClicked()
    {
        OnBackButtonClicked?.Invoke();
    }

    /// <summary>
    /// ボタンのナビゲーション
    /// </summary>
    private void SetupNavigation()
    {
        SetHorizontalNavigation(
            _developersButton,
            _backButton,
            _assetCreditsButton);

        SetHorizontalNavigation(
            _assetCreditsButton,
            _developersButton,
            _backButton);

        SetHorizontalNavigation(
            _backButton,
            _assetCreditsButton,
            _developersButton);
    }

    /// <summary>
    /// ボタンの水平方向のナビゲーションを設定する
    /// </summary>
    /// <param name="selectable"></param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    private static void SetHorizontalNavigation(
        Selectable selectable,
        Selectable left,
        Selectable right)
    {
        Navigation navigation = selectable.navigation;

        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnLeft = left;
        navigation.selectOnRight = right;

        navigation.selectOnUp = null;
        navigation.selectOnDown = null;

        selectable.navigation = navigation;
    }
}
