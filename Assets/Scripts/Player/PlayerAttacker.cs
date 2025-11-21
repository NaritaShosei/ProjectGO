using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerAttacker : MonoBehaviour
{
    private PlayerManager _manager;
    private InputHandler _input;
    private Animator _animator;

    [Header("攻撃のデータ")]
    [SerializeField, Tooltip("コンボの最初の攻撃")] private AttackData _firstComboData;
    [SerializeField, Tooltip("チャージしない攻撃")] private AttackData _heavyAttackData;
    [SerializeField, Tooltip("中チャージ攻撃")] private AttackData _chargedAttackData;
    [SerializeField, Tooltip("強チャージ攻撃")] private AttackData _superChargedAttackData;
    [SerializeField, Tooltip("強チャージ攻撃からの派生攻撃")] private AttackData _superSpinAttackData;
    [SerializeField, Tooltip("回避攻撃")] private AttackData _dodgeAttackData;

    [Header("コンボが途切れる時間")]
    [SerializeField] private float _comboResetTime = 2;
    [SerializeField] private float _superComboResetTime = 1;

    [Header("チャージのデータ")]
    [SerializeField] private ChargeData _chargeData;

    [Header("実際の攻撃を依頼するコンポーネント")]
    [SerializeField] private AttackHandler _attackHandler;

    [System.Serializable]
    private struct ChargeData
    {
        [SerializeField] private float _chargeTime;
        [SerializeField] private float _superChargeTime;

        public float ChargeTime => _chargeTime;
        public float SuperChargeTime => _superChargeTime;
    }

    private AttackData _currentComboData;
    private float _chargeTimer;

    private CancellationTokenSource _cts;
    private CancellationTokenSource _comboResetCts;
    private CancellationTokenSource _heavyComboCts;

    public void Init(PlayerManager manager, InputHandler input, Animator animator)
    {
        _manager = manager;
        _input = input;
        _animator = animator;

        _input.OnLightAttack += HandleComboAttack;
        _input.OnChargeStart += StartCharge;
        _input.OnChargeEnd += ReleaseCharge;

        _manager.OnDead += Dead;

        _currentComboData = _firstComboData;
    }

    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnLightAttack -= HandleComboAttack;
            _input.OnChargeStart -= StartCharge;
            _input.OnChargeEnd -= ReleaseCharge;
        }

        if (_manager != null)
        {
            _manager.OnDead -= Dead;
        }

        CancelAndDisposeCTS();
        CancelAndDisposeComboReset();
        CancelAndDisposeHeavyComboReset();
    }

    #region 弱攻撃コンボ

    /// <summary>
    /// 攻撃を開始する
    /// </summary>
    private async void HandleComboAttack()
    {
        if (!_manager.CanAttack) { return; }

        CancelAndDisposeCTS();
        _cts = new CancellationTokenSource();

        _attackHandler.SetupData(_currentComboData);

        await SafeRun(() => PerformComboAttack(_currentComboData, _cts.Token));
    }

    /// <summary>
    /// 与えられた攻撃のデータを基に攻撃
    /// </summary>
    private async UniTask PerformComboAttack(AttackData attack, CancellationToken token)
    {
        _manager.AddFlags(PlayerStateFlags.Attacking | PlayerStateFlags.MoveLocked | PlayerStateFlags.DodgeLocked);

        if (attack.AnimationClip && _animator)
            _animator.Play(attack.AnimationClip.name);

        Debug.Log($"弱攻撃: {attack.AttackName}");

        // 将来的にはAnimationEventで管理したいが、一旦時間で管理
        await UniTask.Delay(TimeSpan.FromSeconds(attack.MotionDuration), false, PlayerLoopTiming.Update, token);

        // コンボ継続可能なら次段へ
        _currentComboData = attack.NextCombo != null ? attack.NextCombo : _firstComboData;

        _manager.RemoveFlags(PlayerStateFlags.Attacking | PlayerStateFlags.MoveLocked | PlayerStateFlags.DodgeLocked);

        StartComboResetTimer();
    }

    /// <summary>
    /// コンボをリセットするタイマーを始動
    /// </summary>
    private void StartComboResetTimer()
    {
        CancelAndDisposeComboReset();

        _comboResetCts = new CancellationTokenSource();

        _ = RunComboResetTimer(_comboResetCts.Token);
    }

    /// <summary>
    /// タイマーを動かし、コンボをリセット
    /// </summary>
    private async UniTask RunComboResetTimer(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_comboResetTime), cancellationToken: token);
            ResetCombo();
        }
        catch (OperationCanceledException)
        {
            // 途中で新しい攻撃が入力された場合はキャンセルされる
        }
    }

    /// <summary>
    /// コンボをリセット
    /// </summary>
    private void ResetCombo()
    {
        _currentComboData = _firstComboData;
        Debug.Log("コンボリセット！");
    }
    #endregion

    #region 溜め攻撃

    /// <summary>
    /// チャージを開始
    /// </summary>
    private async void StartCharge()
    {
        if (!_manager.CanStartCharge) { return; }
        _manager.AddFlags(PlayerStateFlags.Charging);

        CancelAndDisposeCTS();
        _cts = new CancellationTokenSource();

        _chargeTimer = 0f;
        Debug.Log("溜め開始");

        await SafeRun(() => UpdateTimer(_cts.Token));
    }

    /// <summary>
    /// チャージ終了時の条件に応じて攻撃のデータを設定
    /// </summary>
    private async void ReleaseCharge()
    {
        if (!_manager.IsCharging) return;

        _manager.RemoveFlags(PlayerStateFlags.Charging);

        if (TryHeavyCombo()) { return; }

        CancelAndDisposeCTS();
        _cts = new CancellationTokenSource();

        // 回避攻撃可能時は回避攻撃になる
        AttackData selected = _manager.CanDodgeAttack ? _dodgeAttackData : _heavyAttackData;

        // チャージ時間に応じて攻撃のデータを変更
        if (_chargeTimer > _chargeData.SuperChargeTime)
            selected = _superChargedAttackData;

        else if (_chargeTimer > _chargeData.ChargeTime)
            selected = _chargedAttackData;

        Debug.Log($"{selected.AttackName}発動（時間: {_chargeTimer:F2}s）");

        _attackHandler.SetupData(selected);

        await SafeRun(() => PerformHeavyAttack(selected, _cts.Token));

        CancelAndDisposeComboReset();
        ResetCombo();
    }

    /// <summary>
    /// 与えられた攻撃のデータを基に攻撃
    /// </summary>
    private async UniTask PerformHeavyAttack(AttackData data, CancellationToken token)
    {
        _manager.AddFlags(PlayerStateFlags.Attacking | PlayerStateFlags.MoveLocked | PlayerStateFlags.DodgeLocked);

        if (data.AnimationClip && _animator)
            _animator.Play(data.AnimationClip.name);

        await UniTask.Delay(TimeSpan.FromSeconds(data.MotionDuration), false, PlayerLoopTiming.Update, token);

        // 最大溜め攻撃だった場合は派生可能ウィンドウ ON
        if (data == _superChargedAttackData)
        {
            StartHeavyComboWindow();
        }

        _manager.RemoveFlags(PlayerStateFlags.Attacking | PlayerStateFlags.MoveLocked | PlayerStateFlags.DodgeLocked);
    }

    private void StartHeavyComboWindow()
    {
        _manager.AddFlags(PlayerStateFlags.CanHeavyCombo);

        CancelAndDisposeHeavyComboReset();
        _heavyComboCts = new CancellationTokenSource();

        _ = SafeRun(() => CloseWindow(_superComboResetTime, _heavyComboCts.Token));
    }

    private async UniTask CloseWindow(float time, CancellationToken token)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(time), false, PlayerLoopTiming.Update, token);
        _manager.RemoveFlags(PlayerStateFlags.CanHeavyCombo);
    }


    /// <summary>
    /// 強攻撃からのコンボ派生
    /// </summary>
    private bool TryHeavyCombo()
    {
        if (!_manager.HasFlag(PlayerStateFlags.CanHeavyCombo)) return false;

        // 受け取るデータは最大溜め → 派生先（回転叩きつけ）
        _attackHandler.SetupData(_superSpinAttackData);

        CancelAndDisposeCTS();
        _cts = new CancellationTokenSource();

        _ = SafeRun(() => PerformHeavyAttack(_superSpinAttackData, _cts.Token));

        // 成功したら派生フラグを消す
        _manager.RemoveFlags(PlayerStateFlags.CanHeavyCombo);

        return true;
    }

    /// <summary>
    /// チャージ時間の計算
    /// </summary>
    private async UniTask UpdateTimer(CancellationToken token)
    {
        while (_manager.IsCharging)
        {
            token.ThrowIfCancellationRequested();
            _chargeTimer += Time.deltaTime;
            await UniTask.Yield(token);
        }
    }
    #endregion

    #region 共通ユーティリティ

    /// <summary>
    /// UniTaskすべてでtry/catchをすると長くなるのでまとめる
    /// </summary>
    private async UniTask SafeRun(Func<UniTask> func)
    {
        try { await func(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Debug.LogError($"攻撃中にエラー: {ex}"); }
    }

    /// <summary>
    /// 攻撃のCTSを止める
    /// </summary>
    private void CancelAndDisposeCTS()
    {
        if (_cts is null) { return; }
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    /// <summary>
    /// タイマーのCTSを止める
    /// </summary>
    private void CancelAndDisposeComboReset()
    {
        if (_comboResetCts is null) { return; }
        _comboResetCts.Cancel();
        _comboResetCts.Dispose();
        _comboResetCts = null;
    }

    /// <summary>
    /// チャージ攻撃のコンボCTSを止める
    /// </summary>
    private void CancelAndDisposeHeavyComboReset()
    {
        if (_heavyComboCts is null) { return; }
        _heavyComboCts.Cancel();
        _heavyComboCts.Dispose();
        _heavyComboCts = null;
    }

    #endregion

    #region 外部呼出し

    /// <summary>
    /// 攻撃をキャンセル
    /// </summary>
    public void CancelAttack()
    {
        CancelAndDisposeCTS();

        _manager.RemoveFlags(
            PlayerStateFlags.Attacking |
            PlayerStateFlags.MoveLocked |
            PlayerStateFlags.DodgeLocked |
            PlayerStateFlags.Charging);

        Debug.Log("攻撃をキャンセルするメソッドが実行");
    }

    /// <summary>
    /// 死亡時に行動をキャンセル
    /// </summary>
    public void Dead()
    {
        CancelAttack();
    }
    #endregion
}