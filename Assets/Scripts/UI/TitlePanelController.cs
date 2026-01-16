using UnityEngine;

public class TitlePanelController : MonoBehaviour
{
    [SerializeField]
    private TitlePanelView _titlePanelView;
    private SceneTransitionManager _sceneTransitionManager;

    [Header("シーン遷移先")]
    [SerializeField]
    private string _modeSelectSceneName;
    [SerializeField]
    private string _optionSceneName;

    private void Start()
    {
        _sceneTransitionManager = ServiceLocator.Get<SceneTransitionManager>();

        if (_sceneTransitionManager == null)
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
        _titlePanelView.OnOptionButton += HandleOptionButton;
    }

    private void OnDestroy()
    {
        if (_titlePanelView != null)
        {
            // イベントハンドラの登録解除
            _titlePanelView.OnModeSelectButton -= HandleModeSelectButton;
            _titlePanelView.OnOptionButton -= HandleOptionButton;
        }
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
