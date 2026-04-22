public class WeaponEffectPresenter
{
    public WeaponEffectPresenter(IModeController modeController, WeaponEffectView weaponEffectView)
    {
        _modeController = modeController;
        _weaponEffectView = weaponEffectView;

        modeController.OnModeChanged += _weaponEffectView.Change;
    }

    public void Dispose()
    {
        _modeController.OnModeChanged += _weaponEffectView.Change;
    }

    private IModeController _modeController;
    private WeaponEffectView _weaponEffectView;
}
