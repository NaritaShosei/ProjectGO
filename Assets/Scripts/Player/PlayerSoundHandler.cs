using UnityEngine;

public class PlayerSoundHandler : MonoBehaviour
{
    public void Init(
        PlayerAnimationController animController,
        PlayerStateManager stateManager,
        IModeController modeController,
        AttackExecutor attackExecutor,
        PlayerAttack playerAttack,
        Player player)
    {
        _modeController = modeController;

        if (animController != null)
        {
            animController.OnModeChangeComplete += HandleModeChangeComplete;
            _animController = animController;
        }

        if (modeController != null)
        {
            modeController.OnModeChanged += HandleModeChanged;
        }

        if (stateManager != null)
        {
            stateManager.OnStateChanged += HandleStateChanged;
            _stateManager = stateManager;
        }

        if (attackExecutor != null)
        {
            attackExecutor.OnSwingReady += PlaySwingSE;
            attackExecutor.OnHitResultReady += PlayHitSE;
            _attackExecutor = attackExecutor;
        }

        if (playerAttack != null)
        {
            playerAttack.OnChargeLevelReached += PlayWarriorChargeReadySE;
            _playerAttack = playerAttack;
        }

        if (player != null)
        {
            player.OnDamagedEffect += PlayDamageSE;
            _player = player;
        }
    }

    private IModeController _modeController;
    private PlayerAnimationController _animController;
    private PlayerStateManager _stateManager;
    private AttackExecutor _attackExecutor;
    private PlayerAttack _playerAttack;
    private Player _player;

    private void OnDestroy()
    {
        if (_animController != null)
            _animController.OnModeChangeComplete -= HandleModeChangeComplete;

        if (_modeController != null)
            _modeController.OnModeChanged -= HandleModeChanged;

        if (_stateManager != null)
            _stateManager.OnStateChanged -= HandleStateChanged;

        if (_attackExecutor != null)
        {
            _attackExecutor.OnSwingReady -= PlaySwingSE;
            _attackExecutor.OnHitResultReady -= PlayHitSE;
        }

        if (_playerAttack != null)
            _playerAttack.OnChargeLevelReached -= PlayWarriorChargeReadySE;

        if (_player != null)
            _player.OnDamagedEffect -= PlayDamageSE;
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

    private void HandleModeChanged(PlayerMode mode)
    {
        // アニメーション通知の購読順に依存せず、闘神へ戻った時点で止める。
        if (mode != PlayerMode.Thunder)
            Sound.StopLoopSE(gameObject, SoundCueNames.Player.ThunderElectrify);

        Sound.PlaySE(
            gameObject,
            mode == PlayerMode.Thunder
                ? SoundCueNames.Player.ModeChangeActivate
                : SoundCueNames.Player.ModeChangeDeactivate,
            CueSheetType.Player);
    }

    private void PlayWarriorChargeReadySE(ChargeLevel _)
    {
        Sound.PlaySE(
            gameObject,
            SoundCueNames.Player.WarriorChargeReady,
            CueSheetType.Player);
    }

    private void PlayDamageSE(PlayerDamageEffectContext _)
    {
        Sound.PlaySE(
            gameObject,
            SoundCueNames.Player.Damage,
            CueSheetType.Player);
    }

    private void HandleModeChangeComplete()
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

    private void HandleStateChanged(PlayerState oldState, PlayerState newState)
    {
        if (newState == PlayerState.Dodge && _modeController != null)
        {
            // 入力ではなく回避が成立した時点で鳴らす。拒否された入力では再生しない。
            Sound.PlaySE(gameObject,
                _modeController.CurrentMode == PlayerMode.Thunder
                    ? SoundCueNames.Player.ThunderDodge
                    : SoundCueNames.Player.WarriorRoll,
                CueSheetType.Player);
        }

        if (newState == PlayerState.Dead)
        {
            Sound.StopSE(gameObject);
        }
    }

}
