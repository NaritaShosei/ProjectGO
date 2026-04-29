using System;

public class WeaponEffectPresenter
{
    public WeaponEffectPresenter(IWeaponEffect thunderEffect,
                                 IWeaponEffect warriorEffect,
                                 IModeController modeController)
    {
        _thunderEffect = thunderEffect ?? throw new ArgumentNullException(nameof(thunderEffect));
        _warriorEffect = warriorEffect ?? throw new ArgumentNullException(nameof(warriorEffect));
        _modeController = modeController;

        _thunderEffect.Stop();
        _warriorEffect.Stop();

        _modeController.OnModeChanged += Change;

        // 初期反映
        Change(_modeController.CurrentMode);
    }

    public void Change(PlayerMode mode)
    {
        var next = mode switch
        {
            PlayerMode.Thunder => _thunderEffect,
            PlayerMode.Warrior => _warriorEffect,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (next == _currentEffect)
            return;

        _currentEffect?.Stop();
        _currentEffect = next;
        _currentEffect?.Play();
    }

    public void Dispose()
    {
        _modeController.OnModeChanged -= Change;
    }

    private readonly IWeaponEffect _thunderEffect;
    private readonly IWeaponEffect _warriorEffect;
    private readonly IModeController _modeController;
    private IWeaponEffect _currentEffect;
}
