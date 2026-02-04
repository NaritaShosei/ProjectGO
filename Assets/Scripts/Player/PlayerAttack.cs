using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
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

    // 状態
    private int _currentAttackId = -1;
    private float _lastAttackTime = -999f;
    private float _chargeStartTime = -999f;
    private bool _hasBufferedDodgeAttack = false;
    private bool _isInComboWindow = false;

    // 保留中の攻撃データ
    private AttackData _pendingAttackData;
    private AttackInput? _pendingAttackInput;

    // バッファされたコンボ入力
    private AttackInput? _bufferedComboInput;

    private void OnDestroy()
    {
        _modeController.OnModeChanged -= OnModeChanged;

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
        }
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
    private void PrepareAttack(AttackInput input)
    {
        // 適切な攻撃データを取得
        AttackData attackData = GetNextAttack(input);

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

        // アニメーション再生のみ
        _animationController.PlayAttack(_currentAttackId);
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
    /// アニメーションイベントから呼ばれる攻撃終了関数
    /// </summary>
    private void FinishAttack()
    {
        _stateManager.ChangeState(PlayerState.Idle);
        _pendingAttackData = null;
        _pendingAttackInput = null;

        // バッファされたコンボ入力があれば実行
        if (_bufferedComboInput.HasValue)
        {
            var bufferedInput = _bufferedComboInput.Value;
            _bufferedComboInput = null;

            Debug.Log($"バッファされたコンボを実行: {bufferedInput.AttackType}");
            PrepareAttack(bufferedInput);
        }
        else
        {
            // コンボが途切れた
            ResetCombo();
        }
    }

    /// <summary>
    /// 攻撃データを取得
    /// </summary>
    private AttackData GetNextAttack(AttackInput input)
    {
        // コンボウィンドウ内かつ、次のコンボが存在する場合
        if (_isInComboWindow && _currentAttackId != -1)
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