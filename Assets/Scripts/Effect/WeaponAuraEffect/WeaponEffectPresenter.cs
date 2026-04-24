using System;

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
        _modeController.OnModeChanged -= _weaponEffectView.Change;
    }

    private readonly IModeController _modeController;
    private readonly WeaponEffectView _weaponEffectView;
}
