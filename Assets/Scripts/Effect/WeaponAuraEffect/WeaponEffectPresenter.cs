using System;

public class WeaponEffectPresenter
{
    public WeaponEffectPresenter(IModeController modeController, WeaponEffectView weaponEffectView)
    {
        _modeController = modeController;
        _weaponEffectView = weaponEffectView;

        modeController.OnModeChanged += _weaponEffectView.Change;

        _weaponEffectView.Change(modeController.CurrentMode);
    }

    public void Dispose()
    {
        if (_modeController != null)
            _modeController.OnModeChanged -= _weaponEffectView.Change;
    }

    private IModeController _modeController;
    private WeaponEffectView _weaponEffectView;
}
