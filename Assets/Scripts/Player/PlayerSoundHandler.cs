using UnityEngine;

public class PlayerSoundHandler : MonoBehaviour
{
    public event System.Action<string> OnReviveVoicePlayed;

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
            playerAttack.OnAttackVoiceReady += PlayAttackVoice;
            _playerAttack = playerAttack;
        }

        if (player != null)
        {
            player.OnDamagedEffect += PlayDamageSE;
            player.OnDownRecoveryEnded += PlayReviveVoice;
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
        {
            _playerAttack.OnChargeLevelReached -= PlayWarriorChargeReadySE;
            _playerAttack.OnAttackVoiceReady -= PlayAttackVoice;
        }

        if (_player != null)
        {
            _player.OnDamagedEffect -= PlayDamageSE;
            _player.OnDownRecoveryEnded -= PlayReviveVoice;
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

    private void HandleModeChanged(PlayerMode mode)
    {
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

    private void PlayDamageSE(PlayerDamageEffectContext context)
    {
        Sound.PlaySE(
            gameObject,
            SoundCueNames.Player.Damage,
            CueSheetType.Player);

        if (context.SuppressDamageVoice) return;

        bool useFirst = Random.Range(0, 2) == 0;
        string cueName;
        switch (context.ReactionType)
        {
            case DamageReactionType.Large:
                cueName = useFirst ? SoundCueNames.PlayerVoice.DamageLarge01 : SoundCueNames.PlayerVoice.DamageLarge02;
                break;
            case DamageReactionType.Medium:
                cueName = useFirst ? SoundCueNames.PlayerVoice.DamageMedium01 : SoundCueNames.PlayerVoice.DamageMedium02;
                break;
            default:
                cueName = useFirst ? SoundCueNames.PlayerVoice.DamageSmall01 : SoundCueNames.PlayerVoice.DamageSmall02;
                break;
        }
        Sound.PlaySE(gameObject, cueName, CueSheetType.PlayerVoice);
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
        if (newState == PlayerState.Dead)
        {
            Sound.StopSE(gameObject);
            Sound.PlaySE(gameObject, SoundCueNames.PlayerVoice.Death, CueSheetType.PlayerVoice);
        }
    }

    private void PlayAttackVoice(PlayerMode mode, ChargeLevel chargeLevel, int comboStage)
    {
        // 雷神攻撃はボイスの対応が未確定のため、闘神の3段コンボだけを扱う。
        if (mode != PlayerMode.Warrior) return;

        bool isCharged = chargeLevel > ChargeLevel.None;
        string cueName;
        switch (comboStage)
        {
            case 1:
                cueName = isCharged ? SoundCueNames.PlayerVoice.WarriorAttack04 : SoundCueNames.PlayerVoice.WarriorAttack01;
                break;
            case 2:
                cueName = isCharged ? SoundCueNames.PlayerVoice.WarriorAttack05 : SoundCueNames.PlayerVoice.WarriorAttack02;
                break;
            case 3:
                cueName = isCharged ? SoundCueNames.PlayerVoice.WarriorAttack06 : SoundCueNames.PlayerVoice.WarriorAttack03;
                break;
            default:
                return;
        }
        Sound.PlaySE(gameObject, cueName, CueSheetType.PlayerVoice);
    }

    private void PlayReviveVoice()
    {
        // モブ戦のダウン回復完了時に再生する。復活スキルの死亡キャンセルでは鳴らさない。
        string cueName = Random.Range(0, 2) == 0
            ? SoundCueNames.PlayerVoice.Revive01 : SoundCueNames.PlayerVoice.Revive02;
        Sound.PlaySE(gameObject, cueName, CueSheetType.PlayerVoice);
        OnReviveVoicePlayed?.Invoke(cueName);
    }
}
