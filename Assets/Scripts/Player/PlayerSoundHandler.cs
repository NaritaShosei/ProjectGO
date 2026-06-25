using UnityEngine;

public class PlayerSoundHandler : MonoBehaviour
{
    public void Init(
        PlayerAnimationController animController,
        PlayerStateManager stateManager,
        IModeController modeController,
        AttackExecutor attackExecutor)
    {
        _modeController = modeController;

        if (animController != null)
        {
            animController.OnModeChangeComplete += OnModeChangeComplete;
            _animController = animController;
        }

        if (modeController != null)
        {
            modeController.OnModeChanged += OnModeChanged;
        }

        if (stateManager != null)
        {
            stateManager.OnStateChanged += OnStateChanged;
            _stateManager = stateManager;
        }

        if (attackExecutor != null)
        {
            attackExecutor.OnSwingReady += PlaySwingSE;
            attackExecutor.OnHitResultReady += PlayHitSE;
            _attackExecutor = attackExecutor;
        }
    }

    private void OnDestroy()
    {
        if (_animController != null)
            _animController.OnModeChangeComplete -= OnModeChangeComplete;

        if (_modeController != null)
            _modeController.OnModeChanged -= OnModeChanged;

        if (_stateManager != null)
            _stateManager.OnStateChanged -= OnStateChanged;

        if (_attackExecutor != null)
        {
            _attackExecutor.OnSwingReady -= PlaySwingSE;
            _attackExecutor.OnHitResultReady -= PlayHitSE;
        }
    }

    // ── スイング音 ─────────────────────────────────────

    private void PlaySwingSE(PlayerMode mode)
    {
        switch (mode)
        {
            case PlayerMode.Warrior:
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Player.WeaponSwingWarrior,
                    CueSheetType.Player);
                break;

            case PlayerMode.Thunder:
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Player.WeaponSwingThunder,
                    CueSheetType.Player);
                break;
        }
    }

    // ── ヒット音 ───────────────────────────────────────

    private void PlayHitSE(HitSoundContext ctx)
    {
        if (ctx.IsKill)
        {
            Sound.PlaySE(
                gameObject,
                SoundCueNames.Common.EnemyFinisher,
                CueSheetType.Common);

            return;
        }

        if (ctx.IsArmorBreak)
        {
            Sound.PlaySE(
                gameObject,
                SoundCueNames.Common.ArmorBreak,
                CueSheetType.Common);

            return;
        }

        if (ctx.PlayerMode == PlayerMode.Warrior)
        {
            Sound.PlaySE(
                gameObject,
                ctx.IsArmorHit
                    ? SoundCueNames.Player.HitArmorWarrior
                    : SoundCueNames.Player.HitEnemyWarrior,
                CueSheetType.Player);
        }
        else
        {
            Sound.PlaySE(
                gameObject,
                ctx.IsArmorHit
                    ? SoundCueNames.Player.HitArmorThunder
                    : SoundCueNames.Player.HitEnemyThunder,
                CueSheetType.Player);
        }
    }

    // ── モード変更 ─────────────────────────────────────

    private void OnModeChanged(PlayerMode _)
    {
        Sound.PlaySE(
            gameObject,
            SoundCueNames.Player.ModeChange1,
            CueSheetType.Player);

        Sound.PlaySE(
            gameObject,
            SoundCueNames.Player.ModeChange2,
            CueSheetType.Player);
    }

    private void OnModeChangeComplete()
    {
        if (_modeController.CurrentMode == PlayerMode.Thunder)
        {
            Sound.PlayLoopSE(
                gameObject,
                SoundCueNames.Player.ThunderElectrify,
                CueSheetType.Player);
        }
        else
        {
            Sound.StopLoopSE(
                gameObject,
                SoundCueNames.Player.ThunderElectrify);
        }
    }

    // ── ステート変更 ───────────────────────────────────

    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        if (newState == PlayerState.Dead)
        {
            Sound.StopSE(gameObject);
        }
    }

    private IModeController _modeController;
    private PlayerAnimationController _animController;
    private PlayerStateManager _stateManager;
    private AttackExecutor _attackExecutor;
}
