using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CreditView : MonoBehaviour
{
    public event Action OnBackButtonClicked;

    public void Show()
    {
        gameObject.SetActive(true);

        ShowDevelopers();

        // Showを呼んだフレームの入力を無視する
        _inputStartFrame = Time.frameCount + 1;

        ClearSelection();
    }

    public void Hide()
    {
        ClearSelection();
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

    private bool _showingDevelopers;
    private int _inputStartFrame;

    private void Awake()
    {
        _developersButton.onClick.AddListener(ShowDevelopers);
        _assetCreditsButton.onClick.AddListener(ShowAssetCredits);
        _backButton.onClick.AddListener(HandleBackButtonClicked);

        DisableButtonNavigation(_developersButton);
        DisableButtonNavigation(_assetCreditsButton);
        DisableButtonNavigation(_backButton);
    }

    private void Update()
    {
        if (Time.frameCount < _inputStartFrame)
        {
            return;
        }

        bool backPressed =
            Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
            Gamepad.current?.buttonSouth.wasPressedThisFrame == true;

        bool nextPressed =
            Keyboard.current?.enterKey.wasPressedThisFrame == true ||
            Keyboard.current?.numpadEnterKey.wasPressedThisFrame == true ||
            Gamepad.current?.buttonEast.wasPressedThisFrame == true;

        if (backPressed)
        {
            HandleBackButtonClicked();
        }
        else if (nextPressed)
        {
            ShowNextCredit();
        }
    }

    private static void DisableButtonNavigation(Selectable selectable)
    {
        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.None;
        selectable.navigation = navigation;
    }

    private static void ClearSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // 次のクレジットを表示する
    private void ShowNextCredit()
    {
        if (_showingDevelopers)
        {
            ShowAssetCredits();
        }
        else
        {
            ShowDevelopers();
        }
    }

    // 開発者クレジットを表示する
    private void ShowDevelopers()
    {
        _showingDevelopers = true;
        _creditImage.sprite = _developersImage;

        ClearSelection();
    }

    //アセットクレジットを表示する
    private void ShowAssetCredits()
    {
        _showingDevelopers = false;
        _creditImage.sprite = _assetCreditsImage;

        ClearSelection();
    }

    private void HandleBackButtonClicked()
    {
        ClearSelection();
        OnBackButtonClicked?.Invoke();
    }


    private void OnDestroy()
    {
        _developersButton.onClick.RemoveListener(ShowDevelopers);
        _assetCreditsButton.onClick.RemoveListener(ShowAssetCredits);
        _backButton.onClick.RemoveListener(HandleBackButtonClicked);
    }
}
