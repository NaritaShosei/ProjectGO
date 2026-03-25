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

        // HP と雷ゲージをまとめて渡す
        _playerGaugePresenter = new PlayerGaugePresenter(
            health: player,
            playerStats: player,
            view: _playerGaugeView
        );
    }

    [SerializeField] private PlayerModeView _playerModeView;
    [SerializeField] private PlayerGaugeView _playerGaugeView;

    private IModeController _playerModeController;
    private PlayerModePresenter _playerModePresenter;
    private PlayerGaugePresenter _playerGaugePresenter;

    private void OnDestroy()
    {
        _playerModePresenter?.Dispose();
        _playerGaugePresenter?.Dispose();
    }
}
