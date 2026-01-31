using UnityEngine;

public class ResultUIManager : MonoBehaviour
{
    [Header("リザルトパネルの設定")]
    [SerializeField]
    private ResultPanelView _resultPanelView;

    [Header("表示するパネル")]
    [SerializeField]
    private GameObject _overviewPanel;
    [SerializeField]
    private GameObject _recordPanel;
    [SerializeField]
    private GameObject _buildPanel;

    private CanvasGroup _overviewCanvasGroup;
    private CanvasGroup _recordCanvasGroup;
    private CanvasGroup _buildCanvasGroup;

    private SceneTransitionManager _sceneTransitionManager;
    private ResultPanelModel _resultPanelModel;
    private ResultPanelPresenter _resultPanelPresenter;

    private void Start()
    {
        if (!ServiceLocator.TryGet(out _sceneTransitionManager))
        {
            Debug.LogError("SceneTransitionManager is not registered in ServiceLocator.", this);
            return;
        }

        if (_resultPanelView == null || _overviewPanel == null || _recordPanel == null || _buildPanel == null)
        {
            Debug.LogError("ResultUIManager の参照が未設定です。", this);
            return;
        }

        // Modelの初期化
        _resultPanelModel = new ResultPanelModel();
        // Presenterの初期化
        _resultPanelPresenter = new ResultPanelPresenter(_resultPanelView, _resultPanelModel);
        // イベント登録
        _resultPanelPresenter.OnShowOverview += ShowOverviewPanel;
        _resultPanelPresenter.OnShowRecord += ShowRecordPanel;
        _resultPanelPresenter.OnShowBuild += ShowBuildPanel;
        _resultPanelPresenter.OnTransitionToTitle += TransitionToTitle;

        // CanvasGroupの取得
        if (!_overviewPanel.TryGetComponent(out _overviewCanvasGroup))
        {
            _overviewPanel.AddComponent<CanvasGroup>();
        }
        if (!_recordPanel.TryGetComponent(out _recordCanvasGroup))
        {
            _recordPanel.AddComponent<CanvasGroup>();
        }
        if (!_buildPanel.TryGetComponent(out _buildCanvasGroup))
        {
            _buildPanel.AddComponent<CanvasGroup>();
        }

        // 初期パネル表示
        ShowOverviewPanel();
    }

    private void OnDestroy()
    {
        if (_resultPanelPresenter != null)
        {
            // イベント登録解除
            _resultPanelPresenter.OnShowOverview -= ShowOverviewPanel;
            _resultPanelPresenter.OnShowRecord -= ShowRecordPanel;
            _resultPanelPresenter.OnShowBuild -= ShowBuildPanel;
            _resultPanelPresenter.OnTransitionToTitle -= TransitionToTitle;
        }
    }

    // パネル表示切り替えメソッド
    private void ShowOverviewPanel()
    {
        _overviewPanel.SetActive(true);
        _overviewCanvasGroup.interactable = true;
        _recordPanel.SetActive(false);
        _recordCanvasGroup.interactable = false;
        _buildPanel.SetActive(false);
        _buildCanvasGroup.interactable = false;
    }

    private void ShowRecordPanel()
    {
        _overviewPanel.SetActive(false);
        _overviewCanvasGroup.interactable = false;
        _recordPanel.SetActive(true);
        _recordCanvasGroup.interactable = true;
        _buildPanel.SetActive(false);
        _buildCanvasGroup.interactable = false;
    }

    private void ShowBuildPanel()
    {
        _overviewPanel.SetActive(false);
        _overviewCanvasGroup.interactable = false;
        _recordPanel.SetActive(false);
        _recordCanvasGroup.interactable = false;
        _buildPanel.SetActive(true);
        _buildCanvasGroup.interactable = true;
    }

    private async void TransitionToTitle()
    {
        await _sceneTransitionManager.TransitionToTitle();
    }
}
