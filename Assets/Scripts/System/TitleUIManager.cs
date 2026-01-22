using UnityEngine;

public class TitleUIManager : MonoBehaviour
{
    // タイトル関連
    [SerializeField]
    private TitlePanelView _titlePanelView;

    [Header("シーン遷移先")]
    [SerializeField]
    private string _modeSelectSceneName;

    // オプション関連
    [SerializeField]
    private OptionModel _optionModel;
    [SerializeField]
    private OptionView _optionView;
    [SerializeField]
    private GameObject _optionUIPanel;

    private SceneTransitionManager _sceneTransitionManager;
    private OptionPresenter _optionPresenter;

    private void Start()
    {
        if (!ServiceLocator.TryGet(out _sceneTransitionManager))
        {
            Debug.LogError("SceneTransitionManager is not registered in ServiceLocator.", this);
            return;
        }
        if (_titlePanelView == null)
        {
            Debug.LogError("TitlePanelView is not assigned.", this);
            return;
        }

        // イベントハンドラの登録
        _titlePanelView.OnModeSelectButton += HandleModeSelectButton;
        _titlePanelView.OnOptionButton += OpenOptionMenu;
    }

    private void OnDestroy()
    {
        if (_titlePanelView != null)
        {
            // イベントハンドラの登録解除
            _titlePanelView.OnModeSelectButton -= HandleModeSelectButton;
            _titlePanelView.OnOptionButton -= OpenOptionMenu;
        }
    }

    /// <summary>
    /// 仮実装、ゆくゆくはモードセレクトパネルを表示する予定
    /// </summary>
    private async void HandleModeSelectButton()
    {
        await _sceneTransitionManager.TransitionToScene(_modeSelectSceneName);
    }

    private void CloseOptionButton()
    {
        if (_optionPresenter != null)
        {
            _optionPresenter.OnCloseRequested -= CloseOptionButton;
            _optionPresenter.OnSettingsSaved -= ApplySettingsToGame;
            _optionPresenter.Dispose();
            _optionPresenter = null;
        }
        _optionUIPanel.SetActive(false);
    }

    private void OpenOptionMenu()
    {
        // 既に開いている場合リターン
        if (_optionUIPanel.activeSelf && _optionPresenter != null)
        {
            return;
        }

        _optionUIPanel.SetActive(true);
        _optionPresenter = new OptionPresenter(_optionView, _optionModel);

        _optionPresenter.OnCloseRequested += CloseOptionButton;
        _optionPresenter.OnSettingsSaved += ApplySettingsToGame;
    }


    /// <summary>
    /// 今は使わない予定、未実装
    /// </summary>
    /// <param name="newSettings"></param>
    private void ApplySettingsToGame(GameSetting newSettings)
    {
        // ゲーム全体の設定に反映
        Debug.Log("セーブ機能は未実装です");
    }
}
