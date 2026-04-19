using System;
using UnityEngine;

public class ThunderEffectPresenter : IDisposable
{
    public ThunderEffectPresenter(ThunderEffect thunderEffect, IModeController modeController)
    {
        _thunderEffect = thunderEffect;
        _modeController = modeController;

        modeController.OnModeChanged += _thunderEffect.Play;
    }

    public void Dispose()
    {
        _modeController.OnModeChanged -= _thunderEffect.Play;
    }

    private readonly ThunderEffect _thunderEffect;
    private readonly IModeController _modeController;
}
