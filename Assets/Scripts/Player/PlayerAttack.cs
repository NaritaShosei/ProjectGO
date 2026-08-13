using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerAttack : MonoBehaviour
{
    #region Events

    /// <summary> 攻撃入力があったときに、攻撃の種類やチャージ時間などの情報を通知するイベント </summary>
    public event Action<AttackMoveRequest> OnAttackMoveRequested;
    public event Action OnAttackMoveStopRequested;
    public event Action OnAttackEnded;
    /// <summary> 溜め開始を移動制限のためにPlayerMovementへ通知</summary>
    public event Action OnChargingStarted;
    /// <summary> 溜め終了（攻撃発動 or キャンセル）を通知</summary>
    public event Action OnChargingEnded;
    /// <summary> 溜め段階を通知 </summary>
    public event Action<ChargeLevel> OnChargeLevelReached; // チャージレベルに応じたSEやエフェクトの発動に使用

    #endregion

    #region Public Methods

    /// <summary>
    /// 初期化する。PlayerStateManager, InputHandler, AttackExecutor, IModeController, PlayerAnimationController への参照を受け取る。
    /// </summary>
    public void Init(
        PlayerStateManager playerStateManager,
        InputHandler input,
        AttackExecutor executor,
        IModeController modeController,
        PlayerAnimationController animationController,
        SkillManager skillManager)
    {
        _chargeThresholds = _chargeThresholdSettings
            .OrderByDescending(x => x.TimeThreshold)
            .ToArray();

        _stateManager = playerStateManager;
        _input = input;
        _attackExecutor = executor;
        _modeController = modeController;
        _animationController = animationController;
        _skillManager = skillManager;
        _attackExecutor.OnHitConfirmed += HandleAttackHitConfirmed;

        // R1押し始め → チャージ開始 or 即時攻撃準備
        _input.OnLightAttackPressed += HandleAttackPressed;
        // R1離し → 攻撃発動
        _input.OnLightAttackReleased += HandleAttackReleased;

        _input.OnModeChange += ChangeMode;
        _modeController.OnModeChanged += OnModeChanged;

        _animationController.OnAttackExecute += ExecutePendingAttack;
        _animationController.OnAttackComplete += FinishAttack;
        _animationController.OnComboWindowStart += OnComboWindowStart;
        _animationController.OnComboWindowEnd += OnComboWindowEnd;
        _animationController.OnComboTransition += TryComboTransition;
        _animationController.OnChargeReady += OnChargeReady;

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
            cameraManager.OnLockOnTargetChanged += ChangeLockOnTarget;
    }

    /// <summary>
    /// 回避入力で攻撃を中断する。攻撃のキャンセルとコンボのリセットを行う。
    /// </summary>
    public void InterruptByDodge()
    {
        CancelCharge();
        ClearAttackState();
        OnAttackEnded?.Invoke();
        ResetCombo();
    }

    /// <summary>
    /// コンボが途切れる条件（時間切れなど）で呼ばれる。コンボの状態をリセットする。
    /// </summary>
    public void ResetCombo()
    {
        _currentAttackId = -1;
        _bufferedComboInput = null;
        ClearHomingLock();
    }

    /// <summary>
    /// 被弾などで攻撃が中断される条件で呼ばれる。攻撃とコンボの状態を完全にリセットする。
    /// </summary>
    public void InterruptByDamage()
    {
        CancelCharge();
        ClearAttackState();
        OnAttackEnded?.Invoke();
        _currentAttackId = -1;
    }

    #endregion

    #region Fields

    [SerializeField] private AttackDataRepository _attackRepository;
    [SerializeField] private float _comboResetTime = 1.5f;
    [SerializeField]
    private ChargeThreshold[] _chargeThresholdSettings = new ChargeThreshold[]
    {
        new(1.2f, ChargeLevel.Level3, new ControllerVibrationData(0.35f, 0.15f, 0f)),
        new(0.8f, ChargeLevel.Level2, new ControllerVibrationData(0.20f, 0.10f, 0f)),
        new(0.3f, ChargeLevel.Level1, new ControllerVibrationData(0.10f, 0.05f, 0f)),
    };
    [SerializeField] private LayerMask _homingLayer;

    private PlayerStateManager _stateManager;
    private InputHandler _input;
    private AttackExecutor _attackExecutor;
    private IModeController _modeController;
    private PlayerAnimationController _animationController;
    private SkillManager _skillManager;

    private ChargeThreshold[] _chargeThresholds;

    private int _currentAttackId = -1;
    private float _lastAttackTime = -999f;
    private float _chargeStartTime = -999f;
    private bool _isCharging;
    private bool _isChargeComboFollowUp;

    private bool _isAttackButtonHeld; // 攻撃ボタンが現在押されているかどうか

    private bool _pendingWarriorCharge; // コンボウィンドウ中に入力があり、OnChargeReady待ちの状態

    private bool _canStartCharge;

    private ChargeLevel _currentChargeLevel = ChargeLevel.None;
    private bool _autoFireTriggered = false;

    private bool _isInComboWindow;
    private bool _isComboTransitioned;

    private bool _isHomingActive;
    private float _homingStrength;
    private float _homingRadius;
    private float _homingAngle;
    private Transform _homingTarget;
    private Transform _lockedHomingTarget;
    private bool _isHomingLocked;
    private AttackVariantData _activeAttackVariant;
    private int _currentHitIndex = -1;
    private int _currentMotionHitCount;
    private readonly HashSet<int> _stoppedHitIndices = new();

    private AttackData _pendingAttackData;
    private AttackInput? _pendingAttackInput;
    private AttackInput? _bufferedComboInput;

    private ILockOnTarget _currentLockOnTarget;

    #endregion

    #region UnityMethods

    private void OnDestroy()
    {
        ControllerVibration.Stop();

        if (_modeController != null) _modeController.OnModeChanged -= OnModeChanged;

        if (_input != null)
        {
            _input.OnLightAttackPressed -= HandleAttackPressed;
            _input.OnLightAttackReleased -= HandleAttackReleased;
            _input.OnModeChange -= ChangeMode;
        }

        if (_attackExecutor != null)
            _attackExecutor.OnHitConfirmed -= HandleAttackHitConfirmed;

        if (_animationController != null)
        {
            _animationController.OnAttackExecute -= ExecutePendingAttack;
            _animationController.OnAttackComplete -= FinishAttack;
            _animationController.OnComboWindowStart -= OnComboWindowStart;
            _animationController.OnComboWindowEnd -= OnComboWindowEnd;
            _animationController.OnComboTransition -= TryComboTransition;
            _animationController.OnChargeReady -= OnChargeReady;
        }

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
            cameraManager.OnLockOnTargetChanged -= ChangeLockOnTarget;
    }

    private void Update()
    {
        PerformHoming();
        UpdateCharging();
    }

    #endregion

    #region InputHandlers

    /// <summary>
    /// 攻撃入力の処理。R1押し始めで呼ばれる。現在の状態に応じて、攻撃のチャージ開始やコンボのバッファを行う。
    /// </summary>
    private void HandleAttackPressed()
    {
        // 長押し中に started が再通知されても、新しい攻撃入力として扱わない。
        // 特に雷神モードでは再通知時点でコンボが終了していると、初段が再生されてしまう。
        if (_isAttackButtonHeld) return;

        // すでにチャージ中なら無視
        if (_isCharging) return;

        _canStartCharge = false;
        _isAttackButtonHeld = true;

        if (_stateManager.CurrentState == PlayerState.Attacking && _isInComboWindow)
        {
            // すでにコンボ入力がバッファされているなら無視
            if (_bufferedComboInput.HasValue)
                return;

            // コンボ終端では入力をバッファしない。
            // 終端入力が残ると、Attacking または Charging 状態から復帰できなくなる。
            if (GetNextComboAttack() == null)
                return;

            // 闘神
            if (_modeController.CurrentMode == PlayerMode.Warrior)
            {
                _isCharging = true;
                _currentChargeLevel = ChargeLevel.None;
                _autoFireTriggered = false;
                return;
            }

            // 雷神
            var thunderInput = new AttackInput
            {
                AttackType = AttackType.LightAttack,
                ChargeLevel = ChargeLevel.None
            };

            BufferComboInput(thunderInput);

            return;
        }

        if (!CanAttack()) return;

        _currentChargeLevel = ChargeLevel.None;
        _autoFireTriggered = false;

        // 闘神モードのみ溜め開始を通知（移動制限）
        if (_modeController.CurrentMode == PlayerMode.Warrior)
        {
            _isCharging = true;
            _stateManager.ChangeState(PlayerState.Charging);
            OnChargingStarted?.Invoke();

            var idleChargeData = GetChargeAttackData().GetVariant(ChargeLevel.None);
            if (idleChargeData != null && !string.IsNullOrEmpty(idleChargeData.ChargeAnimationStateName))
            {
                float t = idleChargeData.TransitionDuration < 0 ? 0.1f : idleChargeData.TransitionDuration;
                _animationController.PlayChargeAnimation(idleChargeData.ChargeAnimationStateName, t);
            }
        }
        else
        {
            // 雷神モードは溜めなしで即攻撃準備
            PrepareAttack(new AttackInput
            {
                AttackType = AttackType.LightAttack,
                ChargeLevel = ChargeLevel.None
            });
        }
    }

    /// <summary>
    /// 攻撃入力の処理。R1離しで呼ばれる。チャージ時間に応じた攻撃の発動や、コンボウィンドウ中のバッファからの攻撃準備を行う。
    /// </summary>
    private void HandleAttackReleased()
    {
        _isAttackButtonHeld = false;

        // 雷神はPressed時に処理済み
        if (_modeController.CurrentMode == PlayerMode.Thunder) return;

        // 闘神かつチャージ中でなければ無視
        if (!_isCharging) return;

        // 自動発動済みなら何もしない（UpdateChargingが先に処理した）
        if (_autoFireTriggered) return;

        // TODO:即攻撃ではなくチャージの構えモーションが再生されてから攻撃が発動するようにする

    }
    #endregion

    #region Combo

    /// <summary>
    /// コンボウィンドウ中の攻撃入力をバッファする。コンボ遷移のタイミングでこの入力が存在すれば次の攻撃に繋げる。
    /// </summary>
    /// <param name="input"></param>
    private void BufferComboInput(AttackInput input)
    {
        if (_bufferedComboInput.HasValue) { return; }

        _bufferedComboInput = input;
    }

    /// <summary>
    /// 闘神のチャージ攻撃を発動する。チャージ段階に応じた攻撃を実行し、状態をリセットしてIdleに戻す。
    /// </summary>
    private void FireWarriorAttack(ChargeLevel level)
    {
        ControllerVibration.Stop();
        _isChargeComboFollowUp = _currentAttackId != -1; // コンボの途中でチャージ攻撃が入るかどうか

        _canStartCharge = false;
        _isCharging = false;
        _currentChargeLevel = ChargeLevel.None;

        var input = new AttackInput
        {
            AttackType = AttackType.LightAttack,
            ChargeLevel = level
        };

        // Charging状態のときだけIdleを経由する（コンボ中はAttackingのまま）
        if (_stateManager.CurrentState == PlayerState.Charging)
        {
            OnChargingEnded?.Invoke();
            _stateManager.ChangeState(PlayerState.Idle);

            if (!CanAttack()) return;

            if (_isChargeComboFollowUp)
            {
                PrepareAttack(input, allowCombo: true);
            }
            else
            {
                ResetComboByTime();
                PrepareAttack(input);
            }

            _isChargeComboFollowUp = false;
        }
        else
        {
            // コンボ中（Attacking状態）はCanAttack()を通さず直接遷移
            BufferComboInput(input);
        }
    }

    /// <summary>
    /// 攻撃の準備を行う。攻撃データの取得、状態遷移、ホーミングの設定、移動要求の発行、アニメーション再生などを行う。
    /// </summary>  
    private void PrepareAttack(AttackInput input, bool allowCombo = false)
    {
        AttackData attackData = GetNextAttack(input, allowCombo);

        if (attackData == null)
        {
            ResetCombo();
            return;
        }

        _stateManager.ChangeState(PlayerState.Attacking);
        _currentAttackId = attackData.AttackId;
        _pendingAttackData = attackData;
        _pendingAttackInput = input;
        _lastAttackTime = Time.time;

        var variant = attackData.GetVariant(input.ChargeLevel);

        if (variant == null)
        {
            Debug.LogWarning($"バリアントデータが見つかりませんでした。AttackId: {_currentAttackId}, ChargeLevel: {input.ChargeLevel}");
            return;
        }

        SetupHoming(variant);
        _activeAttackVariant = variant;
        _currentHitIndex = -1;
        _currentMotionHitCount = 0;
        _stoppedHitIndices.Clear();

        FaceAttackTarget();
        RequestAttackMove(variant, false);

        float transition = variant.TransitionDuration < 0 ? 0.1f : variant.TransitionDuration;
        _animationController.PlayAttackBlend(_currentAttackId, variant.AnimationStateName, transition);
    }

    /// <summary>
    /// 攻撃アニメーションの攻撃判定フレームで呼ばれる。ここで実際に攻撃を実行する。
    /// </summary>
    private void ExecutePendingAttack(int hitIndex, int hitCount)
    {
        if (_stateManager.CurrentState != PlayerState.Attacking) return;
        if (_pendingAttackData == null || _pendingAttackInput == null) return;

        _currentHitIndex = hitIndex;
        _currentMotionHitCount = hitCount;
        _attackExecutor.Execute(_pendingAttackData, _pendingAttackInput.Value, _modeController.ModeData, hitIndex);

        // 攻撃判定の発火後は向き追従だけを停止する。
        // 座標追従で使用するターゲット情報は、モーション終了まで保持する。
        _isHomingActive = false;

        // 未命中時はOnHitConfirmedが発火しないため、次ヒットに向けた移動をここで継続する。
        // 同期的にヒット済みの場合は、HandleAttackHitConfirmed側ですでに再開されているため重複させない。
        if (!_stoppedHitIndices.Contains(hitIndex))
            RequestNextHitAttackMove(hitIndex);
    }

    /// <summary>
    /// コンボ遷移のタイミングで呼ばれる。バッファされた攻撃入力があれば次の攻撃に繋げる。次の攻撃が存在しない場合は何もしない。
    /// </summary>
    private void TryComboTransition()
    {
        if (!_bufferedComboInput.HasValue) { return; }
        var bufferedInput = _bufferedComboInput.Value;
        _bufferedComboInput = null;

        AttackData nextAttack = GetNextAttack(bufferedInput, allowCombo: true);
        if (nextAttack == null) { return; }

        _isComboTransitioned = true;
        _currentAttackId = nextAttack.AttackId;
        _pendingAttackData = nextAttack;
        _pendingAttackInput = bufferedInput;
        _lastAttackTime = Time.time;

        var variant = nextAttack.GetVariant(bufferedInput.ChargeLevel);

        if (variant == null) { return; }

        SetupHoming(variant);
        _activeAttackVariant = variant;
        _currentHitIndex = -1;
        _currentMotionHitCount = 0;
        _stoppedHitIndices.Clear();

        FaceAttackTarget();
        RequestAttackMove(variant, false);

        _stateManager.ChangeState(PlayerState.Attacking);
        float transition = variant.TransitionDuration < 0 ? 0.1f : variant.TransitionDuration;
        _animationController.PlayAttackBlend(_currentAttackId, variant.AnimationStateName, transition);
    }

    /// <summary>
    /// 攻撃アニメーションの終了フレームで呼ばれる。コンボ継続の有無をチェックし、次の攻撃に繋げるかIdleに戻す。
    /// </summary>
    private void FinishAttack()
    {
        if (_stateManager.IsDodging() || _stateManager.IsDamaged()) { return; }

        _isHomingActive = false;

        if (_isComboTransitioned)
        {
            _isComboTransitioned = false;
            return;
        }

        OnAttackEnded?.Invoke();

        _pendingAttackData = null;
        _pendingAttackInput = null;
        _activeAttackVariant = null;

        if (_pendingWarriorCharge)
        {
            _stateManager.ChangeState(PlayerState.Charging);
            OnChargingStarted?.Invoke();
            return;
        }

        if (_bufferedComboInput.HasValue)
        {
            var bufferedInput = _bufferedComboInput.Value;
            _bufferedComboInput = null;

            // 入力のバッファ後にスキルの所持状態などが変わり、次段がなくなる場合がある。
            // その場合は次段へ遷移せず、通常どおり攻撃を終了する。
            if (GetNextComboAttack() != null)
            {
                PrepareAttack(bufferedInput, allowCombo: true);
                return;
            }
        }

        _stateManager.ChangeState(PlayerState.Idle);
        ResetCombo();
    }

    /// <summary>
    /// 次の攻撃データを取得する。コンボ継続が可能な場合は次のコンボ攻撃を、そうでない場合は新しい攻撃を取得する。
    /// </summary>
    private AttackData GetNextAttack(AttackInput input, bool allowCombo)
    {
        // コンボ継続チェック
        if ((allowCombo || _isInComboWindow) && _currentAttackId != -1)
        {
            var unlockedIds = _skillManager.GetOwnedSkillIDs();

            var next = _attackRepository.GetNextComboAttack(_currentAttackId, unlockedIds);

            if (next != null) return next;
            // コンボ終端ならnullを返す（新コンボ開始はしない）
            return null;
        }

        // 新規攻撃取得
        var data = _attackRepository.GetAttackData(_modeController.CurrentMode);

        if (data != null)
            return data;

        return null;
    }

    /// <summary>
    /// 現在攻撃可能かどうかをチェックする。PlayerStateManagerの状態に基づいて、攻撃が許可されているかを判断する。
    /// </summary>
    private bool CanAttack() => _stateManager.CanAttack();

    private AttackData GetNextComboAttack()
    {
        if (_currentAttackId == -1) return null;

        var unlockedIds = _skillManager.GetOwnedSkillIDs();
        return _attackRepository.GetNextComboAttack(_currentAttackId, unlockedIds);
    }

    /// <summary>
    /// コンボが途切れる条件をチェックし、必要に応じてコンボをリセットする。ここでは、最後の攻撃から一定時間が経過しているかどうかを確認する。
    /// </summary>
    private void ResetComboByTime()
    {
        if (Time.time - _lastAttackTime > _comboResetTime) ResetCombo();
    }

    /// <summary>
    /// コンボウィンドウの開始と終了を管理する。コンボウィンドウ中は次の攻撃への入力を受け付ける状態になる。
    /// </summary>
    private void OnComboWindowStart() => _isInComboWindow = true;

    /// <summary>
    /// コンボウィンドウの終了を管理する。コンボウィンドウが終了すると、次の攻撃への入力は受け付けなくなる。
    /// </summary>
    private void OnComboWindowEnd()
    {
        // チャージへ分岐する場合も、現在の攻撃のコンボ受付期間は必ず終了する。
        _isInComboWindow = false;

        if (_isCharging && _modeController.CurrentMode == PlayerMode.Warrior)
        {
            _pendingWarriorCharge = true;

            var idleChargeData = GetChargeAttackData().GetVariant(ChargeLevel.None);
            if (idleChargeData != null && !string.IsNullOrEmpty(idleChargeData.ChargeAnimationStateName))
            {
                float t = idleChargeData.TransitionDuration < 0 ? 0.1f : idleChargeData.TransitionDuration;
                _animationController.PlayChargeAnimation(idleChargeData.ChargeAnimationStateName, t);
            }

            return;
        }

        // コンボウィンドウ終了時にチャージ攻撃の準備ができている場合は、コンボ継続ではなくチャージ攻撃に遷移する
    }

    #endregion

    #region Charge

    /// <summary>
    /// 状態をリセットして攻撃をキャンセルする。チャージ状態を解除し、攻撃の準備やコンボの状態もリセットする。
    /// </summary>
    private void ClearAttackState()
    {
        _pendingAttackData = null;
        _pendingAttackInput = null;
        _activeAttackVariant = null;
        _currentHitIndex = -1;
        _currentMotionHitCount = 0;
        _stoppedHitIndices.Clear();
        _bufferedComboInput = null;

        _isInComboWindow = false;
        _isComboTransitioned = false;
        _pendingWarriorCharge = false;

        _isHomingActive = false;

        ClearHomingLock();
    }

    /// <summary>
    /// 攻撃のチャージをキャンセルする。チャージ状態を解除し、必要に応じてIdle状態に戻す。
    /// </summary>
    private void CancelCharge()
    {
        if (!_isCharging) return;
        ControllerVibration.Stop();
        _canStartCharge = false;
        _isCharging = false;
        _pendingWarriorCharge = false;
        _currentChargeLevel = ChargeLevel.None;
        _autoFireTriggered = false;

        if (_stateManager.CurrentState == PlayerState.Charging)
        {
            OnChargingEnded?.Invoke();
            _stateManager.ChangeState(PlayerState.Idle);
        }
    }

    /// <summary>
    /// チャージ時間に応じたChargeLevelを解決する。設定された閾値に基づいて、適切なチャージレベルを返す。
    /// </summary>
    private ChargeLevel ResolveChargeLevel(float chargeTime)
    {
        foreach (var threshold in _chargeThresholds)
        {
            if (chargeTime >= threshold.TimeThreshold)
                return threshold.Level;
        }
        return ChargeLevel.None;
    }

    /// <summary>
    /// チャージ中の更新処理。毎フレーム呼ばれ、チャージ時間に応じたチャージレベルの更新や、Lv3到達での自動発動を行う。
    /// </summary>
    private void UpdateCharging()
    {
        if (!_canStartCharge) return;
        if (!_isCharging || _modeController.CurrentMode != PlayerMode.Warrior) return;

        if (_pendingWarriorCharge) return; // コンボウィンドウ中のチャージはOnComboWindowEndで処理する

        float chargeTime = Time.time - _chargeStartTime;
        ChargeLevel newLevel = ResolveChargeLevel(chargeTime);

        if (!_isAttackButtonHeld)
        {
            FireWarriorAttack(newLevel);
            return;
        }

        // 段階が上がったときだけアニメーションを切り替えてイベント通知
        if (newLevel != _currentChargeLevel)
        {
            _currentChargeLevel = newLevel;

            if (newLevel != ChargeLevel.None)
            {
                PlayChargeVibration(newLevel);
                // チャージ段階に対応したAttackDataのチャージアニメーションを再生
                var chargeData = GetChargeAttackData().GetVariant(newLevel);

                if (chargeData != null && !string.IsNullOrEmpty(chargeData.ChargeAnimationStateName))
                {
                    float transition = chargeData.TransitionDuration < 0 ? 0.1f : chargeData.TransitionDuration;
                    _animationController.PlayChargeAnimation(chargeData.ChargeAnimationStateName, transition);
                }

                OnChargeLevelReached?.Invoke(newLevel);
            }
        }

        // 解放済み最大チャージ段階に達したら自動発動
        if (!_autoFireTriggered && IsMaxChargeLevelReached(newLevel))
        {
            _autoFireTriggered = true;
            FireWarriorAttack(newLevel);
            return;
        }
    }

    /// <summary>
    /// 指定したチャージ段階に対応するAttackDataを取得する。
    /// チャージアニメーション取得用。
    /// </summary>
    /// <summary>
    /// チャージアニメーション取得用。コンボ中であれば次のコンボ段のデータを参照する。
    /// </summary>
    private AttackData GetChargeAttackData()
    {
        // コンボ継続中なら次のコンボ攻撃のデータを使う
        if (_currentAttackId != -1)
        {
            var unlockedIds = _skillManager.GetOwnedSkillIDs();

            var next = _attackRepository.GetNextComboAttack(_currentAttackId, unlockedIds);
            if (next != null) return next;
        }

        // コンボ終端 or 新規攻撃
        return _attackRepository.GetAttackData(_modeController.CurrentMode);
    }

    /// <summary>
    /// 現在のチャージ段階が解放済み最大かどうかを返す
    /// </summary>
    private bool IsMaxChargeLevelReached(ChargeLevel current)
    {
        if (current == ChargeLevel.None) return false;

        ChargeLevel maxLevel = _skillManager.GetMaxChargeLevel(_modeController.CurrentMode);

        return current >= maxLevel;
    }

    /// <summary>
    /// チャージが準備完了したときに呼ばれるイベントハンドラー。チャージ開始を許可し、チャージ開始時間を記録する。
    /// </summary>
    private void OnChargeReady()
    {
        _canStartCharge = true;
        _chargeStartTime = Time.time;

        _pendingWarriorCharge = false;
    }

    private void PlayChargeVibration(ChargeLevel level)
    {
        ControllerVibrationData vibration = _chargeThresholds
            .FirstOrDefault(x => x.Level == level)
            .Vibration;
        if (vibration != null)
        {
            ControllerVibration.PlayContinuous(vibration.Low, vibration.High);
            return;
        }

        switch (level)
        {
            case ChargeLevel.Level1:
                ControllerVibration.PlayContinuous(0.10f, 0.05f);
                break;
            case ChargeLevel.Level2:
                ControllerVibration.PlayContinuous(0.20f, 0.10f);
                break;
            case ChargeLevel.Level3:
                ControllerVibration.PlayContinuous(0.35f, 0.15f);
                break;
        }
    }
    #endregion

    #region Homing

    /// <summary>
    /// 攻撃データに基づいてホーミングの設定を行う。ホーミングが有効な場合は、ホーミングのパラメータをセットし、対象を解決する。無効な場合はホーミングをオフにする。
    /// </summary>
    private void SetupHoming(AttackVariantData data)
    {
        if (data.EnableHoming)
        {
            _isHomingActive = true;
            _homingRadius = data.HomingRadius;
            _homingAngle = data.HomingAngle;
            _homingStrength = data.HomingStrength;

            _homingTarget = ResolveHomingTarget(
                _homingRadius,
                _homingAngle);
        }
        else
        {
            _isHomingActive = false;
            _homingTarget = null;
        }
    }

    /// <summary>
    /// ホーミング対象を見つける。現在のロックオンターゲットが有効ならそれを返し、そうでない場合は周囲の敵から条件に合うものを探して返す。
    /// </summary>
    private Transform FindHomingTarget(float radius, float angle)
    {
        if (_currentLockOnTarget != null)
            return _currentLockOnTarget.GetTargetCenter();

        Transform best = null;
        float bestScore = float.MaxValue;

        if (ServiceLocator.TryGet(out EnemyManager enemyManager))
        {
            var enemies = enemyManager.GetEnemiesInRange(transform.position, radius);
            foreach (var enemy in enemies)
            {
                if (enemy.IsDead) continue;
                var dir = (enemy.GetTargetCenter().position - transform.position).normalized;
                float angleTo = Vector3.Angle(transform.forward, dir);
                if (angleTo > angle) continue;
                float dist = Vector3.Distance(transform.position, enemy.GetTargetCenter().position);
                if (dist < bestScore) { bestScore = dist; best = enemy.GetTargetCenter(); }
            }
            return best;
        }

        var hits = Physics.OverlapSphere(transform.position, radius, _homingLayer);
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

    /// <summary>
    /// ホーミング対象を解決する。ロックオンターゲットが有効ならそれを返し、そうでない場合は周囲から新たにホーミング対象を探す。新しい対象が見つかればホーミングロックする。
    /// </summary>
    private Transform ResolveHomingTarget(float radius, float angle)
    {
        if (_isHomingLocked && _lockedHomingTarget != null)
        {
            if (_lockedHomingTarget.TryGetComponent(out IEnemy e) && !e.IsDead)
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

    /// <summary>
    /// ホーミングロックを解除する。ロックされた対象をクリアし、ロック状態を解除する。
    /// </summary>
    private void ClearHomingLock() { _lockedHomingTarget = null; _isHomingLocked = false; }

    /// <summary>
    /// ホーミング処理を実行する。ホーミングが有効で対象が存在する場合、プレイヤーの向きを対象に向けて徐々に回転させる。
    /// </summary>
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

    #endregion

    #region ModeChange & LockOn

    /// <summary>
    /// モード変更時の処理。モードが変更されたときにコンボをリセットする。これにより、モード変更後の攻撃が新しいコンボとして始まるようになる。
    /// </summary>
    /// <param name="_">イベントに合わせるためのものなので使用しないが引数を付けている</param>
    private void OnModeChanged(PlayerMode _) => ResetCombo();

    /// <summary>
    /// モード変更の入力処理。モード変更が可能な状態であれば、現在のモードに応じて新しいモードに切り替える。雷神モードへの切り替えは即時に行い、闘神モードへの切り替えは状態遷移を伴う。
    /// </summary>
    private void ChangeMode()
    {
        if (!_stateManager.CanModeChange()) { return; }

        var newMode = _modeController.CurrentMode == PlayerMode.Warrior ? PlayerMode.Thunder : PlayerMode.Warrior;

        if (newMode == PlayerMode.Warrior)
        {
            _modeController.SwitchMode(newMode);
            return;
        }

        _stateManager.ChangeState(PlayerState.ModeChanging);
        _modeController.SwitchMode(newMode);
    }

    /// <summary>
    /// ロックオンターゲットの変更処理。カメラマネージャーからロックオンターゲットの変更イベントを受け取ったときに呼ばれる。新しいターゲットが有効であればそれを現在のロックオンターゲットとして設定し、そうでない場合はロックオンターゲットをクリアする。
    /// </summary>
    private void ChangeLockOnTarget(ILockOnTarget target)
    {
        if (target == null)
        {
            _currentLockOnTarget = null;
            return;
        }

        _currentLockOnTarget = (target.IsLockable && target.GetTargetCenter() != null)
            ? target : null;
    }

    /// <summary>
    /// 攻撃の移動要求を発行する。攻撃データに移動が有効な場合に、攻撃の移動要求イベントを発行する。イベントには移動のカーブや距離、速度、対象などの情報が含まれる。
    /// </summary>
    private void RequestAttackMove(AttackVariantData data, bool resume)
    {
        if (!data.EnableMovement) return;

        Vector3 moveDirection = ResolveAttackMoveDirection();
        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        OnAttackMoveRequested?.Invoke(new AttackMoveRequest
        {
            MoveCurve = data.MoveCurve,
            Distance = data.MoveDistance,
            Speed = data.MoveSpeed,
            Duration = data.MoveDuration,
            Resume = resume,
            Direction = moveDirection,
            Target = _homingTarget,
            IsPhantom = data.IsPhantom
        });
    }

    private void FaceAttackTarget()
    {
        Vector3 direction = ResolveAttackMoveDirection();
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private Vector3 ResolveAttackMoveDirection()
    {
        Transform lockOnTarget = GetCurrentLockOnTargetCenter();
        if (lockOnTarget != null)
        {
            Vector3 toTarget = lockOnTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
                return toTarget.normalized;
        }

        if (_homingTarget != null)
        {
            Vector3 toTarget = _homingTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
                return toTarget.normalized;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    private Transform GetCurrentLockOnTargetCenter()
    {
        if (_currentLockOnTarget == null ||
!_currentLockOnTarget.IsLockable ||
_currentLockOnTarget.GetTargetCenter() == null)
        {
            return null;
        }

        return _currentLockOnTarget.GetTargetCenter();
    }

    private void HandleAttackHitConfirmed(int hitIndex)
    {
        if (_activeAttackVariant == null || !_activeAttackVariant.StopOnHit) return;
        if (!_stoppedHitIndices.Add(hitIndex)) return;

        OnAttackMoveStopRequested?.Invoke();

        RequestNextHitAttackMove(hitIndex);
    }

    private void RequestNextHitAttackMove(int hitIndex)
    {
        if (_activeAttackVariant == null || hitIndex < 0) return;

        int nextHitIndex = hitIndex + 1;
        if (nextHitIndex < _currentMotionHitCount)
            RequestAttackMove(_activeAttackVariant, true);
    }
    #endregion
}

[Serializable]
public struct ChargeThreshold
{
    public float TimeThreshold => _timeThreshold;
    public ChargeLevel Level => _level;
    public ControllerVibrationData Vibration => _vibration;

    public ChargeThreshold(
        float timeThreshold,
        ChargeLevel level,
        ControllerVibrationData vibration)
    {
        _timeThreshold = timeThreshold;
        _level = level;
        _vibration = vibration;
    }

    [FormerlySerializedAs("TimeThreshold")]
    [InspectorName("チャージ時間")]
    [SerializeField] private float _timeThreshold;
    [FormerlySerializedAs("Level")]
    [InspectorName("チャージ段階")]
    [SerializeField] private ChargeLevel _level;
    [FormerlySerializedAs("Vibration")]
    [InspectorName("コントローラーの振動")]
    [SerializeField] private ControllerVibrationData _vibration;
}

public struct AttackInput
{
    public AttackType AttackType;
    public ChargeLevel ChargeLevel; // 直接レベルを持つ（ChargeTimeは不要）
}

/// <summary>
/// 攻撃時の移動要求情報
/// </summary>
public struct AttackMoveRequest
{
    public AnimationCurve MoveCurve;
    public float Distance;
    public float Speed;
    public float Duration;
    public bool Resume; // 停止前の経過時間とカーブ位置から再開するか
    public Vector3 Direction;
    public Transform Target; // 攻撃時の一番近い敵
    public bool IsPhantom; // 攻撃がファントムかどうか
}
