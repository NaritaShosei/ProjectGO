using System;
using System.Linq;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    // 攻撃時の移動要求イベント
    public event Action<AttackMoveRequest> OnAttackMoveRequested;

    public void Init(PlayerStateManager playerStateManager,
        InputHandler input,
        AttackExecutor executor,
        IModeController modeController,
        PlayerAnimationController animationController)
    {
        // チャージ時間を基準に降順にソート
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

        // 設定に応じて登録するイベントを変更
        switch (_dodgeAttackConfig.DodgeAttackType)
        {
            case DodgeAttackType.LightAttack:
                _input.OnLightAttack += BufferDodgeAttack;
                break;
            case DodgeAttackType.HeavyAttack:
                _input.OnChargeStart += BufferDodgeAttack;
                break;
        }
    }

    /// <summary>
    /// 回避終了処理
    /// </summary>
    public void FinishDodge()
    {
        // 回避攻撃が有効 & 攻撃ボタンが押されていた場合
        if (_dodgeAttackConfig.IsEnabled && _hasBufferedDodgeAttack)
        {
            PerformDodgeAttack();
        }

        _hasBufferedDodgeAttack = false;
    }

    /// <summary>
    /// コンボをリセット
    /// </summary>
    public void ResetCombo()
    {
        _currentAttackId = -1;
        _bufferedComboInput = null;
        ClearHomingLock();
    }

    // 依存関係
    private PlayerStateManager _stateManager;
    private InputHandler _input;
    private AttackExecutor _attackExecutor;
    private IModeController _modeController;
    private PlayerAnimationController _animationController;
    [SerializeField] private AttackDataRepository _attackRepository;
    [SerializeField] private DodgeAttackConfig _dodgeAttackConfig;

    // 設定
    [SerializeField] private float _comboResetTime = 1.5f;
    [SerializeField]
    private ChargeThreshold[] _chargeThreshold = new ChargeThreshold[]
    {
        new ChargeThreshold { TimeThreshold = 0.5f, Level = ChargeLevel.Level1 },
        new ChargeThreshold { TimeThreshold = 1.5f, Level = ChargeLevel.Level2 }
    };

    [SerializeField] private LayerMask _homingLayer;

    // 状態
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

    // コンボ中に固定されるターゲット
    private Transform _lockedHomingTarget;
    // ロック中フラグ
    private bool _isHomingLocked;

    // 保留中の攻撃データ
    private AttackData _pendingAttackData;
    private AttackInput? _pendingAttackInput;

    // バッファされたコンボ入力
    private AttackInput? _bufferedComboInput;

    private void OnDestroy()
    {
        if (_modeController != null)
        {
            _modeController.OnModeChanged -= OnModeChanged;
        }

        if (_input != null)
        {
            _input.OnLightAttack -= PerformLightAttack;

            _input.OnChargeStart -= StartCharge;

            _input.OnChargeEnd -= ReleaseCharge;

            _input.OnModeChange -= ChangeMode;


            // 設定に応じて解除するイベントを変更
            switch (_dodgeAttackConfig.DodgeAttackType)
            {
                case DodgeAttackType.LightAttack:
                    _input.OnLightAttack -= BufferDodgeAttack;
                    break;
                case DodgeAttackType.HeavyAttack:
                    _input.OnChargeStart -= BufferDodgeAttack;
                    break;
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

    private void Update()
    {
        PerformHoming();
    }

    private void BufferDodgeAttack()
    {
        // 回避中じゃなければ無視
        if (_stateManager.CurrentState != PlayerState.Dodge) { return; }

        // 回避攻撃が有効な場合のみバッファ
        if (_dodgeAttackConfig.IsEnabled)
        {
            _hasBufferedDodgeAttack = true;
        }
    }

    /// <summary>
    /// 回避攻撃を実行
    /// </summary>
    private void PerformDodgeAttack()
    {
        if (!CanAttack()) { return; }

        var input = _dodgeAttackConfig.CreateAttackInput();

        // 回避攻撃はコンボをリセット
        ResetCombo();
        PrepareAttack(input);
    }

    /// <summary>
    /// 弱攻撃を実行
    /// </summary>
    private void PerformLightAttack()
    {
        // 攻撃中でコンボウィンドウ内なら、入力をバッファ
        if (_stateManager.CurrentState == PlayerState.Attacking && _isInComboWindow)
        {
            BufferComboInput(new AttackInput
            {
                AttackType = AttackType.LightAttack,
                ChargeTime = 0f,
            });
            return;
        }

        if (!CanAttack()) { return; }

        ResetComboByTime();

        var input = new AttackInput
        {
            AttackType = AttackType.LightAttack,
            ChargeTime = 0f,
        };

        PrepareAttack(input);
    }
    /// <summary>
    /// チャージ開始
    /// </summary>
    private void StartCharge()
    {
        // 攻撃中でコンボウィンドウ内なら、チャージ入力の可能性があるのでフラグを立てる
        if (_stateManager.CurrentState == PlayerState.Attacking && _isInComboWindow)
        {
            _chargeStartTime = Time.time;
            return;
        }

        if (!CanAttack()) return;
        Debug.Log("チャージ開始");

        _chargeStartTime = Time.time;

        _stateManager.ChangeState(PlayerState.Charging);
    }

    /// <summary>
    /// チャージ解放＆強攻撃
    /// </summary>
    private void ReleaseCharge()
    {
        float chargeTime = Time.time - _chargeStartTime;

        // 攻撃中でコンボウィンドウ内なら、入力をバッファ
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

        if (!_stateManager.IsCharging()) { return; }
        Debug.Log("チャージ終了");

        var input = new AttackInput
        {
            AttackType = AttackType.HeavyAttack,
            ChargeTime = chargeTime,
            WasChargeReleased = true
        };

        _stateManager.ChangeState(PlayerState.Idle);
        PrepareAttack(input);
    }

    /// <summary>
    /// コンボ入力をバッファに保存
    /// </summary>
    private void BufferComboInput(AttackInput input)
    {
        // 既にバッファがある場合は上書き（最新の入力を優先）
        _bufferedComboInput = input;
        Debug.Log($"コンボ入力をバッファ: {input.AttackType}");
    }

    /// <summary>
    /// 攻撃の準備（アニメーション再生まで）
    /// </summary>
    private void PrepareAttack(AttackInput input, bool allowCombo = false)
    {
        // 適切な攻撃データを取得
        AttackData attackData = GetNextAttack(input, allowCombo);

        if (attackData == null)
        {
            Debug.LogWarning($"攻撃データが見つかりません: {input.AttackType}");
            return;
        }

        _stateManager.ChangeState(PlayerState.Attacking);

        // IDの上書き
        _currentAttackId = attackData.AttackId;

        // 攻撃データと入力を保存（アニメーションイベントで実行する）
        _pendingAttackData = attackData;
        _pendingAttackInput = input;

        _lastAttackTime = Time.time;

        if (_pendingAttackData.EnableHoming)
        {
            _isHomingActive = true;
            _homingRadius = _pendingAttackData.HomingRadius;
            _homingAngle = _pendingAttackData.HomingAngle;
            _homingStrength = _pendingAttackData.HomingStrength;
            _homingTarget = ResolveHomingTarget(_homingRadius, _homingAngle);
        }
        else
        {
            _homingTarget = null;
        }

        // 移動要求を発行
        if (attackData.MoveType != AttackMoveType.None)
        {
            var moveRequest = new AttackMoveRequest
            {
                MoveType = attackData.MoveType,
                Distance = attackData.MoveDistance,
                Speed = attackData.MoveSpeed,
                Duration = attackData.MoveDuration,
                Target = _homingTarget,
                StopDistance = attackData.StopOnHit ? attackData.AttackRange : 0,
                IsPhantom = attackData.IsPhantom
            };
            OnAttackMoveRequested?.Invoke(moveRequest);
        }

        // アニメーション再生のみ
        _animationController.PlayAttackBlend(_currentAttackId, attackData.AnimationStateName);
    }

    /// <summary>
    /// アニメーションイベントから呼ばれる実際の攻撃実行
    /// </summary>
    private void ExecutePendingAttack()
    {
        if (_stateManager.CurrentState != PlayerState.Attacking) { return; }

        if (_pendingAttackData == null || _pendingAttackInput == null)
        {
            Debug.LogWarning("保留中の攻撃データがありません");
            return;
        }

        // 実際の攻撃実行
        _attackExecutor.Execute(_pendingAttackData, _pendingAttackInput.Value, _modeController.ModeData);

        Debug.Log($"{_pendingAttackData.Mode}：{_pendingAttackData.AttackName}で攻撃実行");
    }

    /// <summary>
    /// コンボウィンドウ開始時点でバッファがあれば、
    /// ステートが生きているうちにCrossFadeで遷移
    /// </summary>
    private void TryComboTransition()
    {
        if (!_bufferedComboInput.HasValue) { return; }

        var bufferedInput = _bufferedComboInput.Value;
        _bufferedComboInput = null;

        AttackData nextAttack = GetNextAttack(bufferedInput, allowCombo: true);
        if (nextAttack == null) { return; }

        _isComboTransitioned = true; // 追加

        _currentAttackId = nextAttack.AttackId;
        _pendingAttackData = nextAttack;
        _pendingAttackInput = bufferedInput;
        _lastAttackTime = Time.time;

        // PrepareAttack と同等の副作用を適用
        if (nextAttack.EnableHoming)
        {
            _isHomingActive = true;
            _homingRadius = nextAttack.HomingRadius;
            _homingAngle = nextAttack.HomingAngle;
            _homingStrength = nextAttack.HomingStrength;
            _homingTarget = ResolveHomingTarget(_homingRadius, _homingAngle);
        }
        else
        {
            _homingTarget = null;
        }

        if (nextAttack.MoveType != AttackMoveType.None)
        {
            var moveRequest = new AttackMoveRequest
            {
                MoveType = nextAttack.MoveType,
                Distance = nextAttack.MoveDistance,
                Speed = nextAttack.MoveSpeed,
                Duration = nextAttack.MoveDuration,
                Target = _homingTarget,
                StopDistance = nextAttack.StopOnHit ? nextAttack.AttackRange : 0,
                IsPhantom = nextAttack.IsPhantom
            };

            OnAttackMoveRequested?.Invoke(moveRequest);
        }

        _stateManager.ChangeState(PlayerState.Attacking);

        if (nextAttack.TransitionDuration < 0)
        {
            // 遷移時間が負の場合はデフォルトの遷移時間を使用
            _animationController.PlayAttackBlend(
                _currentAttackId,
                nextAttack.AnimationStateName
            );
            return;
        }

        _animationController.PlayAttackBlend(
            _currentAttackId,
            nextAttack.AnimationStateName,
            nextAttack.TransitionDuration
        );
    }

    /// <summary>
    /// アニメーションイベントから呼ばれる攻撃終了関数
    /// </summary>
    private void FinishAttack()
    {
        _isHomingActive = false;

        // コンボ遷移済みの場合はpendingをクリアしない
        if (_isComboTransitioned)
        {
            _isComboTransitioned = false;
            return; // 次のステートに引き継ぐ
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

    /// <summary>
    /// 攻撃データを取得
    /// </summary>
    private AttackData GetNextAttack(AttackInput input, bool allowCombo)
    {
        // コンボウィンドウ内かつ、次のコンボが存在する場合
        if ((allowCombo || _isInComboWindow) && _currentAttackId != -1)
        {
            // 現在の攻撃データを取得
            var currentAttack = _attackRepository.GetAttackById(_currentAttackId);

            // 次のコンボが存在するか
            if (currentAttack != null && currentAttack.NextComboAttackId != -1)
            {
                var nextAttack = _attackRepository.GetAttackById(currentAttack.NextComboAttackId);

                if (nextAttack != null && IsCompatibleAttack(nextAttack, input))
                {
                    return nextAttack;
                }
            }

            else
            {
                // コンボウィンドウ内でも次のコンボが存在しない場合は無効
                return null;
            }
        }

        // 新規コンボ開始
        ChargeLevel chargeLevel = input.GetChargeLevel(_chargeThreshold);
        return _attackRepository.GetAttackData(_modeController.CurrentMode, input.AttackType, 0, chargeLevel);
    }

    private bool IsCompatibleAttack(AttackData attack, AttackInput input)
    {
        // 攻撃タイプが一致するか
        if (attack.AttackType != input.AttackType) { return false; }

        return true;
    }

    /// <summary>
    /// 攻撃可能かチェック
    /// </summary>
    private bool CanAttack()
    {
        // 状態マネージャーで攻撃可能状態かチェック
        return _stateManager.CanAttack();
    }

    /// <summary>
    /// 時間を基準にコンボをリセット
    /// </summary>
    private void ResetComboByTime()
    {
        // コンボリセット判定
        if (Time.time - _lastAttackTime > _comboResetTime)
        {
            ResetCombo();
        }
    }

    private Transform FindHomingTarget(float radius, float angle)
    {
        var hits = Physics.OverlapSphere(transform.position, radius, _homingLayer);

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out IEnemy _)) { continue; }

            var dir = (hit.transform.position - transform.position).normalized;
            float angleTo = Vector3.Angle(transform.forward, dir);
            if (angleTo > angle) { continue; }

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < bestScore)
            {
                bestScore = dist;
                best = hit.transform;
            }
        }

        return best;
    }

    /// <summary>
    /// ホーミングターゲットを決定する。
    /// コンボ中かつロック済みの場合は固定ターゲットを返す。
    /// </summary>
    private Transform ResolveHomingTarget(float radius, float angle)
    {
        if (_isHomingLocked && _lockedHomingTarget != null)
        {
            // 死亡チェック：IEnemy経由でチェック
            if (_lockedHomingTarget != null && _lockedHomingTarget.TryGetComponent(out IEnemy enemy) && !enemy.IsDead)
            {
                return _lockedHomingTarget; // 固定ターゲットを返す
            }

            // 死亡していたら次のターゲットへ
            _lockedHomingTarget = null;
        }

        // 新規検索
        var newTarget = FindHomingTarget(radius, angle);

        // コンボ中なら固定
        if (newTarget != null && _currentAttackId != -1)
        {
            _lockedHomingTarget = newTarget;
            _isHomingLocked = true;
        }

        return newTarget;
    }

    /// <summary>
    /// コンボ終了時にホーミングロックを解除
    /// </summary>
    private void ClearHomingLock()
    {
        _lockedHomingTarget = null;
        _isHomingLocked = false;
    }

    private void PerformHoming()
    {
        if (!_isHomingActive) { return; }

        if (_homingTarget == null) { return; }

        var dir = _homingTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0f) { return; }

        var targetRot = Quaternion.LookRotation(dir);

        // strength = 5〜15くらいが気持ちいい
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * _homingStrength
        );
    }

    private void OnComboWindowStart()
    {
        _isInComboWindow = true;
    }

    private void OnComboWindowEnd()
    {
        _isInComboWindow = false;
    }

    private void ChangeMode()
    {
        if (!_stateManager.CanModeChange()) { return; }

        var newMode = _modeController.CurrentMode == PlayerMode.Warrior
            ? PlayerMode.Thunder
            : PlayerMode.Warrior;

        _stateManager.ChangeState(PlayerState.ModeChanging);

        _modeController.SwitchMode(newMode);
    }

    private void OnModeChanged(PlayerMode newMode)
    {
        // モード変更時の処理(エフェクト、SE再生など)
        Debug.Log($"モード変更: {newMode}");

        ResetCombo();
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
