using UnityEngine;

public class TitlePanelController : MonoBehaviour
{
    [SerializeField]
    private TitlePanelView _titlePaneView;
    private SceneTransitionManager _sceneTransitionManager;

    [Header("シーン遷移先")]
    [SerializeField]
    private string _modeSelectSceneName;
    [SerializeField]
    private string _optionSceneName;

    private void Start()
    {
        _sceneTransitionManager = ServiceLocator.Get<SceneTransitionManager>();
        // イベントハンドラの登録
        _titlePaneView.OnModeSelectButton += HandleModeSelectButton;
        _titlePaneView.OnOptionButton += HandleOptionButton;
    }

    private void OnDestroy()
    {
        // イベントハンドラの登録解除
        _titlePaneView.OnModeSelectButton -= HandleModeSelectButton;
        _titlePaneView.OnOptionButton -= HandleOptionButton;
    }

    private void HandleModeSelectButton()
    {
        _sceneTransitionManager.TransitionToScene(_modeSelectSceneName);
    }
    private void HandleOptionButton()
    {
        _sceneTransitionManager.TransitionToScene(_optionSceneName);
    }
}
