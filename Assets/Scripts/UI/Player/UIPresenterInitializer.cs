using UnityEngine;
/// <summary>
/// Presenter を生成・初期化する 
/// </summary>
public class UIPresenterInitializer : MonoBehaviour
{
    //モード管理
    [SerializeField] private PlayerModeController _playerModeController;
    //UI表示
    [SerializeField] private PlayerModeView _playerModeView;
    //Presenter
    private PlayerModePresenter _presenter;
    private void Start()
    {
        //Presenter生成
        _presenter = new PlayerModePresenter(_playerModeController,_playerModeView);

        _presenter.Initialize();
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
    }
}
