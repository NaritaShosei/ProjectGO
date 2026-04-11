using System;
using System.Linq;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public event Action<AttackMoveRequest> OnAttackMoveRequested;

    public void Init(PlayerStateManager playerStateManager,
        InputHandler input,
        AttackExecutor executor,
        IModeController modeController,
        PlayerAnimationController animationController)
    {
        _chargeThreshold = _chargeThreshold.OrderByDescending(x => x.TimeThreshold).ToArray();

        _stateManager = playerStateManager;
        _input = input;
        _attackExecutor = executor;
        _modeController = modeController;
        _animationController = animationController;

        _input.OnLightAttack += PerformLightAttack;
        _input.OnChargeStart += StartCharge;
        _input.OnChargeEnd += ReleaseCharge;
        _input.OnModeChange += ChangeMode;

        _modeController.OnModeChanged += OnModeChanged;

        _animationController.OnAttackExecute += ExecutePendingAttack;
        _animationController.OnAttackComplete += FinishAttack;
        _animationController.OnComboWindowStart += OnComboWindowStart;
        _animationController.OnComboWindowEnd += OnComboWindowEnd;
        _animationController.OnComboTransition += TryComboTransition;

        switch (_dodgeAttackConfig.DodgeAttackType)
        {
            case DodgeAttackType.LightAttack:
                _input.OnLightAttack += BufferDodgeAttack;
                break;
            case DodgeAttackType.HeavyAttack:
                _input.OnChargeStart += BufferDodgeAttack;
                break;
        }

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
            cameraManager.OnLockOnTargetChanged += ChangeLockOnTarget;
    }

    /// <summary>
    /// 回避終了後のDodgeAttack判定。PlayerMovementのOnEndDodgeから呼ばれる。
    /// </summary>
    public void FinishDodge()
    {
        if (_dodgeAttackConfig.IsEnabled && _hasBufferedDodgeAttack)
            PerformDodgeAttack();

        _hasBufferedDodgeAttack = false;
    }

    /// <summary>
    /// 回避による攻撃中断。PlayerMovementから呼ばれる。
    /// 攻撃移動・コンボをリセットしてIdle相当の状態にする。
    /// ステート変更はPlayerMovementが行うため、ここではAttack内部状態のみリセット。
    /// </summary>
    public void InterruptByDodge()
    {
        // アニメーション終了イベントが来ても処理しないようペンディングをクリア
        _pendingAttackData = null;
        _pendingAttackInput = null;
        _bufferedComboInput = null;
        _isInComboWindow = false;
        _isComboTransitioned = false;
        _isHomingActive = false;
        ClearHomingLock();

        // 攻撃移動があれば強制キャンセル（OnAttackMoveRequestedのCTSはPlayerMovementが持つため、
        // PlayerMovementのHandleAttackMoveがOnEndDodgeで自動的に解決する）
    }

    public void ResetCombo()
    {
        _currentAttackId = -1;
        _bufferedComboInput = null;
        ClearHomingLock();
    }

    /// <summary>
    /// 被弾による攻撃中断。Player.TakeDamageから呼ばれる。
    /// 全ての攻撃内部状態をリセットしてIdle相当の状態にする。
    /// </summary>
    public void InterruptByDamage()
    {
        _pendingAttackData = null;
        _pendingAttackInput = null;
        _bufferedComboInput = null;
        _isInComboWindow = false;
        _isComboTransitioned = false;
        _isHomingActive = false;
        _currentAttackId = -1;   // コンボチェーンもリセット
        ClearHomingLock();
    }

    // ── Inspector ──────────────────────────────────────────
    [SerializeField] private AttackDataRepository _attackRepository;
    [SerializeField] private DodgeAttackConfig _dodgeAttackConfig;
    [SerializeField] private float _comboResetTime = 1.5f;
    [SerializeField]
    private ChargeThreshold[] _chargeThreshold = new ChargeThreshold[]
    {
        new ChargeThreshold { TimeThreshold = 0.5f, Level = ChargeLevel.Level1 },
        new ChargeThreshold { TimeThreshold = 1.5f, Level = ChargeLevel.Level2 }
    };
    [SerializeField] private LayerMask _homingLayer;

    // ── Private State ──────────────────────────────────────
    private PlayerStateManager _stateManager;
    private InputHandler _input;
    private AttackExecutor _attackExecutor;
    private IModeController _modeController;
    private PlayerAnimationController _animationController;

    private int _currentAttackId = -1;
    private float _lastAttackTime = -999f;
    private float _chargeStartTime = -999f;
    private bool _hasBufferedDodgeAttack = false;
    private bool _isInComboWindow = false;
    private bool _isComboTransitioned = false;

    private bool _isHomingActive;
    private float _homingStrength;
    private float _homingRadius;
    private float _homingAngle;
    private Transform _homingTarget;
    private Transform _lockedHomingTarget;
    private bool _isHomingLocked;

    private AttackData _pendingAttackData;
    private AttackInput? _pendingAttackInput;
    private AttackInput? _bufferedComboInput;

    private ILockOnTarget _currentLockOnTarget;

    // ── Lifecycle ──────────────────────────────────────────

    private void OnDestroy()
    {
        if (_modeController != null) _modeController.OnModeChanged -= OnModeChanged;

        if (_input != null)
        {
            _input.OnLightAttack -= PerformLightAttack;
            _input.OnChargeStart -= StartCharge;
            _input.OnChargeEnd -= ReleaseCharge;
            _input.OnModeChange -= ChangeMode;

            switch (_dodgeAttackConfig.DodgeAttackType)
            {
                case DodgeAttackType.LightAttack: _input.OnLightAttack -= BufferDodgeAttack; break;
                case DodgeAttackType.HeavyAttack: _input.OnChargeStart -= BufferDodgeAttack; break;
            }
        }

        if (_animationController != null)
        {
            _animationController.OnAttackExecute -= ExecutePendingAttack;
            _animationController.OnAttackComplete -= FinishAttack;
            _animationController.OnComboWindowStart -= OnComboWindowStart;
            _animationController.OnComboWindowEnd -= OnComboWindowEnd;
            _animationController.OnComboTransition -= TryComboTransition;
        }
    }

    private void Update() => PerformHoming();

    // ── 入力ハンドラ ───────────────────────────────────────

    private void BufferDodgeAttack()
    {
        if (_stateManager.CurrentState != PlayerState.Dodge) return;
        if (_dodgeAttackConfig.IsEnabled) _hasBufferedDodgeAttack = true;
    }

    private void PerformDodgeAttack()
    {
        if (!CanAttack()) return;
        var input = _dodgeAttackConfig.CreateAttackInput();
        ResetCombo();
        PrepareAttack(input);
    }

    private void PerformLightAttack()
    {
        if (_stateManager.CurrentState == PlayerState.Attacking && _isInComboWindow)
        {
            BufferComboInput(new AttackInput { AttackType = AttackType.LightAttack, ChargeTime = 0f });
            return;
        }
        if (!CanAttack()) return;
        ResetComboByTime();
        PrepareAttack(new AttackInput { AttackType = AttackType.LightAttack, ChargeTime = 0f });
    }

    private void StartCharge()
    {
        if (_stateManager.CurrentState == PlayerState.Attacking && _isInComboWindow)
        {
            _chargeStartTime = Time.time;
            return;
        }
        if (!CanAttack()) return;
        _chargeStartTime = Time.time;
        _stateManager.ChangeState(PlayerState.Charging);
    }

    private void ReleaseCharge()
    {
        float chargeTime = Time.time - _chargeStartTime;
        if (_stateManager.CurrentState == PlayerState.Attacking && _isInComboWindow)
        {
            BufferComboInput(new AttackInput
            {
                AttackType = AttackType.HeavyAttack,
                ChargeTime = chargeTime,
                WasChargeReleased = true
            });
            return;
        }
        if (!_stateManager.IsCharging()) return;
        var input = new AttackInput { AttackType = AttackType.HeavyAttack, ChargeTime = chargeTime, WasChargeReleased = true };
        _stateManager.ChangeState(PlayerState.Idle);
        PrepareAttack(input);
    }

    private void BufferComboInput(AttackInput input) => _bufferedComboInput = input;

    // ── 攻撃実行 ───────────────────────────────────────────

    private void PrepareAttack(AttackInput input, bool allowCombo = false)
    {
        AttackData attackData = GetNextAttack(input, allowCombo);
        if (attackData == null) { Debug.LogWarning($"攻撃データが見つかりません: {input.AttackType}"); return; }

        _stateManager.ChangeState(PlayerState.Attacking);
        _currentAttackId = attackData.AttackId;
        _pendingAttackData = attackData;
        _pendingAttackInput = input;
        _lastAttackTime = Time.time;

        if (attackData.EnableHoming)
        {
            _isHomingActive = true;
            _homingRadius = attackData.HomingRadius;
            _homingAngle = attackData.HomingAngle;
            _homingStrength = attackData.HomingStrength;
            _homingTarget = ResolveHomingTarget(_homingRadius, _homingAngle);
        }
        else
        {
            _homingTarget = null;
        }

        if (attackData.MoveType != AttackMoveType.None)
        {
            OnAttackMoveRequested?.Invoke(new AttackMoveRequest
            {
                MoveType = attackData.MoveType,
                Distance = attackData.MoveDistance,
                Speed = attackData.MoveSpeed,
                Duration = attackData.MoveDuration,
                Target = _homingTarget,
                StopDistance = attackData.StopOnHit ? attackData.AttackRange : 0,
                IsPhantom = attackData.IsPhantom
            });
        }

        _animationController.PlayAttackBlend(_currentAttackId, attackData.AnimationStateName);
    }

    private void ExecutePendingAttack()
    {
        if (_stateManager.CurrentState != PlayerState.Attacking) return;
        if (_pendingAttackData == null || _pendingAttackInput == null) return;
        _attackExecutor.Execute(_pendingAttackData, _pendingAttackInput.Value, _modeController.ModeData);
    }

    private void TryComboTransition()
    {
        if (!_bufferedComboInput.HasValue) return;
        var bufferedInput = _bufferedComboInput.Value;
        _bufferedComboInput = null;

        AttackData nextAttack = GetNextAttack(bufferedInput, allowCombo: true);
        if (nextAttack == null) return;

        _isComboTransitioned = true;
        _currentAttackId = nextAttack.AttackId;
        _pendingAttackData = nextAttack;
        _pendingAttackInput = bufferedInput;
        _lastAttackTime = Time.time;

        if (nextAttack.EnableHoming)
        {
            _isHomingActive = true;
            _homingRadius = nextAttack.HomingRadius;
            _homingAngle = nextAttack.HomingAngle;
            _homingStrength = nextAttack.HomingStrength;
            _homingTarget = ResolveHomingTarget(_homingRadius, _homingAngle);
        }
        else { _homingTarget = null; }

        if (nextAttack.MoveType != AttackMoveType.None)
        {
            OnAttackMoveRequested?.Invoke(new AttackMoveRequest
            {
                MoveType = nextAttack.MoveType,
                Distance = nextAttack.MoveDistance,
                Speed = nextAttack.MoveSpeed,
                Duration = nextAttack.MoveDuration,
                Target = _homingTarget,
                StopDistance = nextAttack.StopOnHit ? nextAttack.AttackRange : 0,
                IsPhantom = nextAttack.IsPhantom
            });
        }

        _stateManager.ChangeState(PlayerState.Attacking);
        _animationController.PlayAttackBlend(
            _currentAttackId,
            nextAttack.AnimationStateName,
            nextAttack.TransitionDuration < 0 ? 0.1f : nextAttack.TransitionDuration);
    }

    private void FinishAttack()
    {
        // 回避・ダメージリアクション中はAttackの終了処理をスキップ
        if (_stateManager.CurrentState == PlayerState.Dodge ||
            _stateManager.IsDamaged())
            return;

        _isHomingActive = false;

        if (_isComboTransitioned)
        {
            _isComboTransitioned = false;
            return;
        }

        _pendingAttackData = null;
        _pendingAttackInput = null;

        if (_bufferedComboInput.HasValue)
        {
            var bufferedInput = _bufferedComboInput.Value;
            _bufferedComboInput = null;
            PrepareAttack(bufferedInput, allowCombo: true);
        }
        else
        {
            _stateManager.ChangeState(PlayerState.Idle);
            ResetCombo();
        }
    }

    // ── ヘルパー ───────────────────────────────────────────

    private AttackData GetNextAttack(AttackInput input, bool allowCombo)
    {
        if ((allowCombo || _isInComboWindow) && _currentAttackId != -1)
        {
            var currentAttack = _attackRepository.GetAttackById(_currentAttackId);
            if (currentAttack != null && currentAttack.NextComboAttackId != -1)
            {
                var nextAttack = _attackRepository.GetAttackById(currentAttack.NextComboAttackId);
                if (nextAttack != null && IsCompatibleAttack(nextAttack, input)) return nextAttack;
            }
            else return null;
        }

        ChargeLevel chargeLevel = input.GetChargeLevel(_chargeThreshold);
        return _attackRepository.GetAttackData(_modeController.CurrentMode, input.AttackType, 0, chargeLevel);
    }

    private bool IsCompatibleAttack(AttackData attack, AttackInput input)
        => attack.AttackType == input.AttackType;

    private bool CanAttack() => _stateManager.CanAttack();

    private void ResetComboByTime()
    {
        if (Time.time - _lastAttackTime > _comboResetTime) ResetCombo();
    }

    private Transform FindHomingTarget(float radius, float angle)
    {
        if(_currentLockOnTarget != null)
        {
            return _currentLockOnTarget.GetTargetCenter();
        }

        var hits = Physics.OverlapSphere(transform.position, radius, _homingLayer);
        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out IEnemy enemy) || enemy.IsDead) continue;
            var dir = (hit.transform.position - transform.position).normalized;
            float angleTo = Vector3.Angle(transform.forward, dir);
            if (angleTo > angle) continue;
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < bestScore) { bestScore = dist; best = hit.transform; }
        }
        return best;
    }

    private Transform ResolveHomingTarget(float radius, float angle)
    {
        if (_isHomingLocked && _lockedHomingTarget != null)
        {
            if (_lockedHomingTarget.TryGetComponent(out IEnemy enemy) && !enemy.IsDead)
                return _lockedHomingTarget;
            ClearHomingLock();
        }
        var newTarget = FindHomingTarget(radius, angle);
        if (newTarget != null && _currentAttackId != -1)
        {
            _lockedHomingTarget = newTarget;
            _isHomingLocked = true;
        }
        return newTarget;
    }

    private void ClearHomingLock() { _lockedHomingTarget = null; _isHomingLocked = false; }

    private void PerformHoming()
    {
        if (!_isHomingActive || _homingTarget == null) return;
        var dir = _homingTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * _homingStrength);
    }

    private void OnComboWindowStart() => _isInComboWindow = true;
    private void OnComboWindowEnd() => _isInComboWindow = false;

    private void ChangeMode()
    {
        if (!_stateManager.CanModeChange()) return;
        var newMode = _modeController.CurrentMode == PlayerMode.Warrior
            ? PlayerMode.Thunder : PlayerMode.Warrior;
        if (newMode == PlayerMode.Warrior) { _modeController.SwitchMode(newMode); return; }
        _stateManager.ChangeState(PlayerState.ModeChanging);
        _modeController.SwitchMode(newMode);
    }

    private void OnModeChanged(PlayerMode newMode)
    {
        ResetCombo();
    }

    private void ChangeLockOnTarget(ILockOnTarget target)
    {
        _currentLockOnTarget = target;
    }
}

[Serializable]
public struct ChargeThreshold
{
    public float TimeThreshold;
    public ChargeLevel Level;
}

public struct AttackInput
{
    public AttackType AttackType;
    public float ChargeTime;           // チャージした時間
    public bool WasChargeReleased;     // チャージが解放されたか

    public ChargeLevel GetChargeLevel(ChargeThreshold[] thresholds)
    {
        if (thresholds == null || thresholds.Length == 0)
            return ChargeLevel.None;

        // 降順でソート済みと仮定
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (ChargeTime >= thresholds[i].TimeThreshold)
                return thresholds[i].Level;
        }

        return ChargeLevel.None;
    }
}

/// <summary>
/// 攻撃時の移動要求情報
/// </summary>
public struct AttackMoveRequest
{
    public AttackMoveType MoveType;
    public float Distance;
    public float Speed;
    public float Duration;
    public Transform Target; // 攻撃時の一番近い敵
    public float StopDistance; // 敵がいるときに攻撃を止める距離
    public bool IsPhantom; // 攻撃がファントムかどうか
}
