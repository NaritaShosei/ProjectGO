using UnityEngine;

/// <summary>
/// Presenter を生成・初期化する 
/// </summary>
public class InGameUIInitializer : MonoBehaviour
{
    public void Init(Player player)
    {
        if (player.TryGetComponent(out IModeController modeController))
        {
            _playerModeController = modeController;
            _playerModePresenter = new PlayerModePresenter(_playerModeController, _playerModeView);
        }
        else
        {
            Debug.LogError("PlayerにIModeControllerが見つかりませんでした。");
        }

        _playerGaugePresenter = new PlayerGaugePresenter(health: player, stamina: player, _playerGaugeView);
    }

    //UI表示
    [SerializeField] private PlayerModeView _playerModeView;
    [SerializeField] private PlayerGaugeView _playerGaugeView;

    private IModeController _playerModeController;

    //Presenter
    private PlayerModePresenter _playerModePresenter;
    private PlayerGaugePresenter _playerGaugePresenter;

    private void OnDestroy()
    {
        if (_playerModeController != null)
        {
            _playerModePresenter?.Dispose();
        }

        if (_playerGaugePresenter != null)
        {
            _playerGaugePresenter?.Dispose();
        }
    }
}
