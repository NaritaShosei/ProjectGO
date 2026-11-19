using System;

/// <summary>
/// モード変更を監視し、Warrior から Thunder への切替時にポストプロセス演出を再生する。
/// </summary>
public sealed class ModeChangePostProcessEffectPresenter : IDisposable
{
    public ModeChangePostProcessEffectPresenter(
        ModeChangePostProcessEffectPlayer effectPlayer,
        IModeController modeController,
        JustDodgeEffectPlayer justDodgeEffectPlayer)
    {
        _effectPlayer = effectPlayer ?? throw new ArgumentNullException(nameof(effectPlayer));
        _modeController = modeController ?? throw new ArgumentNullException(nameof(modeController));
        _previousMode = modeController.CurrentMode;
        _justDodgeEffectPlayer = justDodgeEffectPlayer;

        if (_justDodgeEffectPlayer != null)
            _justDodgeEffectPlayer.OnEffectPlaying += _effectPlayer.StopPostProcess;

        _modeController.OnModeChanged += OnModeChanged;
        _effectPlayer.OnEffectEnabled += HandleEffectEnabled;
        _effectPlayer.ChangeHammerEmission(_previousMode, immediate: true);
    }

    public void Dispose()
    {
        if (_justDodgeEffectPlayer != null)
        {
            _justDodgeEffectPlayer.OnEffectPlaying -= _effectPlayer.StopPostProcess;
            _justDodgeEffectPlayer.Stop();
        }

        _modeController.OnModeChanged -= OnModeChanged;
        _effectPlayer.OnEffectEnabled -= HandleEffectEnabled;
        _effectPlayer.Stop();
    }

    private readonly ModeChangePostProcessEffectPlayer _effectPlayer;
    private readonly IModeController _modeController;
    private readonly JustDodgeEffectPlayer _justDodgeEffectPlayer;
    private PlayerMode _previousMode;

    private void OnModeChanged(PlayerMode newMode)
    {
        bool shouldPlay = _previousMode == PlayerMode.Warrior
            && newMode == PlayerMode.Thunder;
        bool shouldRevertTint = _previousMode == PlayerMode.Thunder
            && newMode == PlayerMode.Warrior;

        _previousMode = newMode;

        // 雷神切替の画面演出は Vignette を共有するため、再生前にジャスト回避を終了する。
        // 闘神へ戻る場合は切替演出が Vignette を使わないため、回避演出を継続する。
        if (shouldPlay && _justDodgeEffectPlayer != null)
            _justDodgeEffectPlayer.Stop();

        // Playerが無効な間は状態だけ追跡し、演出の再生やマテリアル操作を行わない。
        if (!_effectPlayer.isActiveAndEnabled) return;

        _effectPlayer.ChangeHammerEmission(newMode);

        if (shouldPlay)
        {
            _effectPlayer.Play().Forget();
            _effectPlayer.PlayColorTint().Forget();
        }

        if (shouldRevertTint)
        {
            _effectPlayer.StopPostProcess();
            _effectPlayer.StopColorTint().Forget();
        }
    }

    private void HandleEffectEnabled()
    {
        _previousMode = _modeController.CurrentMode;
        _effectPlayer.ChangeHammerEmission(_previousMode, immediate: true);
    }

}
