using UnityEngine;

public class PlayerEffectInitializer : MonoBehaviour
{
    public void Init(Player player)
    {
        if (player.TryGetComponent(out IModeController modeController))
        {
            _playerModeController = modeController;
            _thunderEffectPresenter = new ThunderEffectPresenter(_thunderEffect, _playerModeController);
            _weaponEffectPresenter = new WeaponEffectPresenter(modeController, _weaponEffectView);
        }
        else
        {
            Debug.LogError("PlayerにIModeControllerが見つかりませんでした。");
        }
    }

    [SerializeField] private ThunderEffectView _thunderEffect;
    [SerializeField] private WeaponEffectView _weaponEffectView;

    private IModeController _playerModeController;
    private ThunderEffectPresenter _thunderEffectPresenter;
    private WeaponEffectPresenter _weaponEffectPresenter;

    private void OnDestroy()
    {
        _thunderEffectPresenter?.Dispose();
        _weaponEffectPresenter?.Dispose();
    }
}
