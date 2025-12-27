using System.Linq;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public void Init(PlayerStateManager playerStateManager, InputHandler input)
    {
        // チャージ時間を基準に降順にソート
        _chargeThreshold.OrderByDescending(x => x.TimeThreshold);

        _stateManager = playerStateManager;

        input.OnLightAttack += PerformLightAttack;
        input.OnChargeStart += StartCharge;
        input.OnChargeEnd += ReleaseCharge;
    }

    /// <summary>
    /// コンボをリセット
    /// </summary>
    public void ResetCombo()
    {
        _currentComboIndex = 0;
    }

    // 依存関係
    private PlayerStateManager _stateManager;
    [SerializeField] private AttackExecutor _attackExecutor;
    [SerializeField] private AttackDataRepository _attackRepository;

    // 設定
    [SerializeField] private float _comboResetTime = 1.5f;
    [SerializeField]
    private ChargeThreshold[] _chargeThreshold = new ChargeThreshold[]
    {
        new ChargeThreshold { TimeThreshold = 0.5f, Level = ChargeLevel.Level1 },
        new ChargeThreshold { TimeThreshold = 1.5f, Level = ChargeLevel.Level2 }
    };

    // 状態
    private int _currentComboIndex = 0;
    private float _lastAttackTime = -999f;
    private float _chargeStartTime = -999f;
    private CombatMode _currentMode = CombatMode.Warrior;

    /// <summary>
    /// 弱攻撃を実行
    /// </summary>
    private void PerformLightAttack()
    {
        if (!CanAttack()) return;

        ResetComboByTime();

        var input = new AttackInput
        {
            AttackType = AttackType.LightAttack,
            ChargeTime = 0f,
        };

        ExecuteAttack(input);
    }

    /// <summary>
    /// チャージ開始
    /// </summary>
    private void StartCharge()
    {
        if (!CanAttack()) return;
        Debug.Log("チャージ開始");

        ResetCombo();

        _chargeStartTime = Time.time;

        _stateManager.ChangeState(PlayerState.Charging);
    }

    /// <summary>
    /// チャージ解放＆強攻撃
    /// </summary>
    private void ReleaseCharge()
    {
        if (!_stateManager.IsCharging()) return;
        Debug.Log("チャージ終了");
        float chargeTime = Time.time - _chargeStartTime;

        var input = new AttackInput
        {
            AttackType = AttackType.HeavyAttack,
            ChargeTime = chargeTime,
            WasChargeReleased = true
        };

        _stateManager.ChangeState(PlayerState.Idle);
        ExecuteAttack(input);
    }

    /// <summary>
    /// 回避攻撃を実行
    /// </summary>
    private void PerformDodgeAttack()
    {
        if (!CanAttack()) return;

        var input = new AttackInput
        {
            AttackType = AttackType.DodgeAttack,
            ChargeTime = 0f,
        };

        // 回避攻撃はコンボをリセット
        ResetCombo();
        ExecuteAttack(input);
    }

    /// <summary>
    /// モードを切り替え
    /// </summary>
    private void SwitchMode(CombatMode newMode)
    {
        _currentMode = newMode;
        ResetCombo();
    }

    /// <summary>
    /// 攻撃を実行（内部処理）
    /// </summary>
    private void ExecuteAttack(AttackInput input)
    {
        // 適切な攻撃データを取得
        AttackData_main attackData = GetAttackData(input);

        if (attackData == null)
        {
            Debug.LogWarning($"攻撃データが見つかりません: {input.AttackType}, コンボ{_currentComboIndex}");
            return;
        }

        // 攻撃実行
        bool success = _attackExecutor.Execute(attackData, input);

        if (success)
        {
            _lastAttackTime = Time.time;

            // コンボを進める
            if (attackData.NextComboAttackId != -1)
            {
                _currentComboIndex++;
            }
            else
            {
                // コンボ終端に到達
                _currentComboIndex = 0;
            }
        }
    }

    /// <summary>
    /// 攻撃データを取得
    /// </summary>
    private AttackData_main GetAttackData(AttackInput input)
    {
        ChargeLevel chargeLevel = input.GetChargeLevel(_chargeThreshold);

        return _attackRepository.GetAttackData(
            _currentMode,
            input.AttackType,
            _currentComboIndex,
            chargeLevel
        );
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
}

[System.Serializable]
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
        for (int i = thresholds.Length - 1; i >= 0; i--)
        {
            if (ChargeTime >= thresholds[i].TimeThreshold)
                return thresholds[i].Level;
        }

        return ChargeLevel.None;
    }
}