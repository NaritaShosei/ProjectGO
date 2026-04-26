using System;

public class WeaponEffectPresenter
{
    public WeaponEffectPresenter(WeaponEffectView weaponEffectView,IModeController modeController)
    {
        _modeController = modeController;
        _weaponEffectView = weaponEffectView;

        modeController.OnModeChanged += _weaponEffectView.Change;

        //初期設定しないとChangeが呼ばれないからエフェクトが出ない
        _weaponEffectView.Change(modeController.CurrentMode);
    }

    public void Dispose()
    {
        _modeController.OnModeChanged -= _weaponEffectView.Change;
    }

    private readonly IModeController _modeController;
    private readonly WeaponEffectView _weaponEffectView;
}
