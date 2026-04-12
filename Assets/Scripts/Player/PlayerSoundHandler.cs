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

        animController.OnModeChangeComplete += OnModeChanged;
        stateManager.OnStateChanged += OnStateChanged;
        attackExecutor.OnSwingReady += PlaySwingSE;
        attackExecutor.OnHitResultReady += PlayHitSE;

        _animController = animController;
        _stateManager = stateManager;
        _attackExecutor = attackExecutor;
    }

    private void OnDestroy()
    {
        if (_animController != null)
        {
            _animController.OnModeChangeComplete -= OnModeChanged;
        }

        if (_stateManager != null) _stateManager.OnStateChanged -= OnStateChanged; 
        
        if (_attackExecutor != null)
        {
            _attackExecutor.OnSwingReady -= PlaySwingSE;
            _attackExecutor.OnHitResultReady -= PlayHitSE;
        }
    }

    // ── スイング音 ────────────────────────────────────────
    private void PlaySwingSE(PlayerMode mode)
    {
        if (mode == PlayerMode.Warrior)
            Sound.PlayTousnSE(gameObject, SoundCueNames.Tousin.HammerSwing);
        else
        {
            Sound.PlayRaijinSE(gameObject, SoundCueNames.Raijin.HammerSwing);
            Sound.PlayRaijinSE(gameObject, SoundCueNames.Raijin.Attack);
        }
    }

    // ── ヒット音 ──────────────────────────────────────────
    private void PlayHitSE(HitSoundContext ctx)
    {
        // 共通SE（モード問わず優先）
        if (ctx.IsKill) { Sound.PlayCommonSE(gameObject, SoundCueNames.Common.Finish); return; }
        if (ctx.IsArmorBreak) { Sound.PlayCommonSE(gameObject, SoundCueNames.Common.ArmorBreak); return; }

        // モード別ヒット音
        if (ctx.PlayerMode == PlayerMode.Warrior)
        {
            // 弱点 = 鎧ヒット / 非弱点 = 生身ヒット（闘神は鎧が弱点）
            var cue = ctx.IsWeakPoint
                ? SoundCueNames.Tousin.ArmorHit
                : SoundCueNames.Tousin.BodyHit;
            Sound.PlayTousnSE(gameObject, cue);
        }
        else
        {
            // 弱点 = 生身ヒット / 非弱点 = 鎧ヒット（雷神は生身が弱点）
            var cue = ctx.IsWeakPoint
                ? SoundCueNames.Raijin.BodyHit
                : SoundCueNames.Raijin.ArmorHit;
            Sound.PlayRaijinSE(gameObject, cue);
        }
    }

    // ── 帯電ループ ────────────────────────────────────────
    private void OnModeChanged()
    {
        var mode = _modeController.CurrentMode;

        if (mode == PlayerMode.Thunder)
            Sound.PlayRaijinLoopSE(gameObject, SoundCueNames.Raijin.Taiden);
        else
            Sound.StopLoopSE(gameObject, SoundCueNames.Raijin.Taiden);
    }

    // ── ステート変化 ──────────────────────────────────────
    private void OnStateChanged(PlayerState old, PlayerState next)
    {
        if (next == PlayerState.Dead)
            Sound.StopSE(gameObject);
    }

    // ── フィールド ────────────────────────────────────────
    private IModeController _modeController;
    private PlayerAnimationController _animController;
    private PlayerStateManager _stateManager;
    private AttackExecutor _attackExecutor;
}
