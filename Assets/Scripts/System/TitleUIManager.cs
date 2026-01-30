using System;
using UnityEngine;

public class TitleUIManager : MonoBehaviour
{
    // タイトル関連
    [SerializeField]
    private TitlePanelView _titlePanelView;

    [Header("オプションパネル設定")]
    [SerializeField]
    private OptionModel _optionModel;
    [SerializeField]
    private OptionView _optionView;
    [SerializeField]
    private GameObject _optionUIPanel;

    [Header("モードセレクトパネル設定")]
    [SerializeField]
    private ModeSelectModel _modeSelectModel;
    [SerializeField]
    private ModeSelectView _modeSelectView;
    [SerializeField]
    private GameObject _modeSelectPanel;
    private ModeSelectPresenter _modeSelectPresenter;

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
        _titlePanelView.OnModeSelectButton += OpenModeSelectPanel;
        _titlePanelView.OnOptionButton += OpenOptionPanel;
    }

    private void OnDestroy()
    {
        if (_titlePanelView != null)
        {
            // イベントハンドラの登録解除
            _titlePanelView.OnModeSelectButton -= OpenModeSelectPanel;
            _titlePanelView.OnOptionButton -= OpenOptionPanel;
        }
    }

    /// <summary>
    /// モードセレクトパネルを開く
    /// </summary>
    private void OpenModeSelectPanel()
    {
        if (_modeSelectPanel.activeSelf && _modeSelectPresenter != null)
        {
            return;
        }

        // モードセレクトパネルを表示
        _modeSelectPanel.SetActive(true);
        _modeSelectPresenter = new ModeSelectPresenter(_modeSelectView, _modeSelectModel);
        _modeSelectView.ShowThisPanel();

        // イベントハンドラの登録
        _modeSelectPresenter.OnSceneSelected += SceneTransitionToScene;
        _modeSelectPresenter.OnModeSelectCloseRequested += CloseModeSelectPanel;
    }

    /// <summary>
    /// モードセレクトパネルを閉じる
    /// </summary>
    private void CloseModeSelectPanel()
    {
        if (_modeSelectPresenter != null)
        {
            _modeSelectPresenter.OnSceneSelected -= SceneTransitionToScene;
            _modeSelectPresenter.OnModeSelectCloseRequested -= CloseModeSelectPanel;
            _modeSelectPresenter.Dispose();
            _modeSelectPresenter = null;
        }
        _modeSelectPanel.SetActive(false);
        _titlePanelView.ShowThisPanel();
    }

    private async void SceneTransitionToScene(string sceneName)
    {
        try
        {
            await _sceneTransitionManager.TransitionToScene(sceneName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"シーン遷移中にエラーが発生しました: {ex.Message}", this);
        }
    }

    /// <summary>
    /// オプション画面を開く
    /// </summary>
    private void OpenOptionPanel()
    {
        // 既に開いている場合リターン
        if (_optionUIPanel.activeSelf && _optionPresenter != null)
        {
            return;
        }

        _optionUIPanel.SetActive(true);
        _optionView.ShowThisPanel();
        _optionPresenter = new OptionPresenter(_optionView, _optionModel);

        _optionPresenter.OnOptionCloseRequested += CloseOptionPanel;
        _optionPresenter.OnSettingsSaved += ApplySettingsToGame;
    }

    /// <summary>
    /// オプション画面を閉じる
    /// </summary>
    private void CloseOptionPanel()
    {
        if (_optionPresenter != null)
        {
            _optionPresenter.OnOptionCloseRequested -= CloseOptionPanel;
            _optionPresenter.OnSettingsSaved -= ApplySettingsToGame;
            _optionPresenter.Dispose();
            _optionPresenter = null;
        }
        _optionUIPanel.SetActive(false);
        _titlePanelView.ShowThisPanel();
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
