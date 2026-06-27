using UnityEngine;

public class PlayerUIInitializer : MonoBehaviour
{
    public void Init(Player player)
    {
        if (player == null)
        {
            Debug.LogError("[PlayerUIInitializer] Player is null.", this);
            return;
        }

        if (_playerModeView != null && player.TryGetComponent(out IModeController modeController))
        {
            _playerModeController = modeController;
            _playerModePresenter = new PlayerModePresenter(_playerModeController, _playerModeView);
        }
        else
        {
            Debug.LogError("[PlayerUIInitializer] PlayerModeView or IModeController is missing.", this);
        }

        if (_playerGaugeView != null)
        {
            _playerGaugePresenter = new PlayerGaugePresenter(
                health: player,
                playerStats: player,
                view: _playerGaugeView
            );
        }
        else
        {
            Debug.LogError("[PlayerUIInitializer] PlayerGaugeView is missing.", this);
        }

        // ロックオンマーカー初期化
        if (_lockOnMarkerView != null && ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            _lockOnMarkerPresenter = new LockOnMarkerPresenter(cameraManager, _lockOnMarkerView, destroyCancellationToken);
        }
        else
        {
            Debug.LogError("[PlayerUIInitializer] LockOnMarkerView or CameraManager is missing. LockOnMarker is disabled.", this);
        }
    }

    [SerializeField] private PlayerModeView _playerModeView;
    [SerializeField] private PlayerGaugeView _playerGaugeView;
    [SerializeField] private LockOnMarkerView _lockOnMarkerView;

    private IModeController _playerModeController;
    private PlayerModePresenter _playerModePresenter;
    private PlayerGaugePresenter _playerGaugePresenter;
    private LockOnMarkerPresenter _lockOnMarkerPresenter;

    private void OnDestroy()
    {
        _playerModePresenter?.Dispose();
        _playerGaugePresenter?.Dispose();
        _lockOnMarkerPresenter?.Dispose();
    }
}
