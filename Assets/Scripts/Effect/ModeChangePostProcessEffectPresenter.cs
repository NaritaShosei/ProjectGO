using System;

/// <summary>
/// モード変更を監視し、Warrior から Thunder への切替時にポストプロセス演出を再生する。
/// </summary>
public sealed class ModeChangePostProcessEffectPresenter : IDisposable
{
    public ModeChangePostProcessEffectPresenter(
        ModeChangePostProcessEffectPlayer effectPlayer,
        IModeController modeController)
    {
        _effectPlayer = effectPlayer ?? throw new ArgumentNullException(nameof(effectPlayer));
        _modeController = modeController ?? throw new ArgumentNullException(nameof(modeController));
        _previousMode = modeController.CurrentMode;

        _modeController.OnModeChanged += OnModeChanged;
        _effectPlayer.OnEffectEnabled += HandleEffectEnabled;
        _effectPlayer.ChangeHammerEmission(_previousMode, immediate: true);
    }

    public void Dispose()
    {
        _modeController.OnModeChanged -= OnModeChanged;
        _effectPlayer.OnEffectEnabled -= HandleEffectEnabled;
        _effectPlayer.Stop();
    }

    private readonly ModeChangePostProcessEffectPlayer _effectPlayer;
    private readonly IModeController _modeController;
    private PlayerMode _previousMode;

    private void OnModeChanged(PlayerMode newMode)
    {
        bool shouldPlay = _previousMode == PlayerMode.Warrior
            && newMode == PlayerMode.Thunder;

        _previousMode = newMode;

        // Playerが無効な間は状態だけ追跡し、演出の再生やマテリアル操作を行わない。
        if (!_effectPlayer.isActiveAndEnabled) return;

        _effectPlayer.ChangeHammerEmission(newMode);

        if (shouldPlay)
            _effectPlayer.Play().Forget();
    }

    private void HandleEffectEnabled()
    {
        _previousMode = _modeController.CurrentMode;
        _effectPlayer.ChangeHammerEmission(_previousMode, immediate: true);
    }

}
