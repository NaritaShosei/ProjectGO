using UnityEngine;

public class UIPresenterInitializer : MonoBehaviour
{
    [SerializeField] private PlayerModeController _playerModeController;
    [SerializeField] private PlayerModeView _playerModeView;

    private PlayerModePresenter _presenter;
    void Start()
    {
        _presenter = new PlayerModePresenter(_playerModeController,_playerModeView);

        _presenter.Initialize();
    }
    private void OnDestroy()
    {
        _presenter?.Dispose();
    }
}
