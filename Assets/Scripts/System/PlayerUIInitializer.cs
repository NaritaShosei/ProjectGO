using UnityEngine;

public class PlayerUIInitializer : MonoBehaviour
{
    public void Init(Player player)
    {
        if (player.TryGetComponent(out IModeController modeController))
        {
            _playerModeController = modeController;
            _playerModePresenter = new PlayerModePresenter(_playerModeController, _playerModeView);
            _thunderEffectPresenter = new ThunderEffectPresenter(_thunderEffect, _playerModeController);
        }
        else
        {
            Debug.LogError("PlayerにIModeControllerが見つかりませんでした。");
        }

        _playerGaugePresenter = new PlayerGaugePresenter(
            health: player,
            playerStats: player,
            view: _playerGaugeView
        );

        // ロックオンマーカー初期化
        if (ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            _lockOnMarkerPresenter = new LockOnMarkerPresenter(cameraManager, _lockOnMarkerView, destroyCancellationToken);
        }
        else
        {
            Debug.LogError("CameraManagerが見つかりませんでした。LockOnMarkerは無効です。");
        }
    }

    [SerializeField] private PlayerModeView _playerModeView;
    [SerializeField] private PlayerGaugeView _playerGaugeView;
    [SerializeField] private LockOnMarkerView _lockOnMarkerView;
    [SerializeField] private ThunderEffectView _thunderEffect;

    private IModeController _playerModeController;
    private PlayerModePresenter _playerModePresenter;
    private PlayerGaugePresenter _playerGaugePresenter;
    private LockOnMarkerPresenter _lockOnMarkerPresenter;
    private ThunderEffectPresenter _thunderEffectPresenter;

    private void OnDestroy()
    {
        _playerModePresenter?.Dispose();
        _playerGaugePresenter?.Dispose();
        _lockOnMarkerPresenter?.Dispose();
        _thunderEffectPresenter?.Dispose();
    }
}
