using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerAttacker : MonoBehaviour
{
    private PlayerManager _manager;
    private InputHandler _input;

    [Header("攻撃のデータ")]
    [SerializeField, Tooltip("コンボの最初の攻撃")] private AttackData _firstComboData;
    [SerializeField, Tooltip("チャージしない攻撃")] private AttackData _heavyAttackData;
    [SerializeField, Tooltip("中チャージ攻撃")] private AttackData _chargedAttackData;
    [SerializeField, Tooltip("強チャージ攻撃")] private AttackData _superChargedAttack;

    [Header("コンボが途切れる時間")]
    [SerializeField] private float _comboResetTime = 2;

    [Header("チャージのデータ")]
    [SerializeField] private ChargeData _chargeData;

    [System.Serializable]
    private struct ChargeData
    {
        [SerializeField] private float _chargeTime;
        [SerializeField] private float _superChargeTime;

        public float ChargeTime => _chargeTime;
        public float SuperChargeTime => _superChargeTime;
    }

    [Header("Animator")]
    [SerializeField] private Animator _animator;

    private AttackData _currentComboData;
    private bool _isAttacking;
    private bool _isCharging;
    private float _chargeTimer;

    private CancellationTokenSource _cts;
    private CancellationTokenSource _comboResetCts;

    public void Init(PlayerManager manager, InputHandler input)
    {
        _manager = manager;
        _input = input;

        _input.OnLightAttack += HandleComboAttack;
        _input.OnChargeStart += StartCharge;
        _input.OnChargeEnd += ReleaseCharge;

        _currentComboData = _firstComboData;
    }

    #region 弱攻撃コンボ

    private async void HandleComboAttack()
    {
        if (_isAttacking || _isCharging) { return; }

        CancelAndDisposeCTS();
        _cts = new CancellationTokenSource();

        await SafeRun(() => PerformComboAttack(_currentComboData, _cts.Token));
    }

    private async UniTask PerformComboAttack(AttackData attack, CancellationToken token)
    {
        _isAttacking = true;

        if (attack.AnimationClip && _animator)
            _animator.Play(attack.AnimationClip.name);

        Debug.Log($"弱攻撃: {attack.AttackName}");

        // 将来的にはAnimationEventで管理したいが、一旦時間で管理
        await UniTask.Delay(TimeSpan.FromSeconds(attack.MotionDuration), false, PlayerLoopTiming.Update, token);

        // コンボ継続可能なら次段へ
        _currentComboData = attack.NextCombo != null ? attack.NextCombo : _firstComboData;

        _isAttacking = false;

        StartComboResetTimer();
    }

    private void StartComboResetTimer()
    {
        CancelAndDisposeComboReset();

        _comboResetCts = new CancellationTokenSource();

        _ = RunComboResetTimer(_comboResetCts.Token);
    }

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

    private void ResetCombo()
    {
        _currentComboData = _firstComboData;
        Debug.Log("コンボリセット！");
    }
    #endregion

    #region 溜め攻撃

    private async void StartCharge()
    {
        if (_isAttacking || _isCharging) { return; }
        _isCharging = true;

        CancelAndDisposeCTS();
        _cts = new CancellationTokenSource();

        _chargeTimer = 0f;
        Debug.Log("溜め開始");

        await SafeRun(() => UpdateTimer(_cts.Token));
    }

    private async void ReleaseCharge()
    {
        if (!_isCharging) return;

        _isCharging = false;

        CancelAndDisposeCTS();
        _cts = new CancellationTokenSource();

        AttackData selected = _heavyAttackData;

        if (_chargeTimer > _chargeData.SuperChargeTime)
            selected = _superChargedAttack;

        else if (_chargeTimer > _chargeData.ChargeTime)
            selected = _chargedAttackData;

        Debug.Log($"{selected.AttackName}発動（時間: {_chargeTimer:F2}s）");

        await SafeRun(() => PerformHeavyAttack(selected, _cts.Token));

        CancelAndDisposeComboReset();
        ResetCombo();
    }

    private async UniTask PerformHeavyAttack(AttackData data, CancellationToken token)
    {
        _isAttacking = true;

        if (data.AnimationClip && _animator)
            _animator.Play(data.AnimationClip.name);

        await UniTask.Delay(TimeSpan.FromSeconds(data.MotionDuration), false, PlayerLoopTiming.Update, token);

        _isAttacking = false;
    }

    private async UniTask UpdateTimer(CancellationToken token)
    {
        while (_isCharging)
        {
            token.ThrowIfCancellationRequested();
            _chargeTimer += Time.deltaTime;
            await UniTask.Yield(token);
        }
    }
    #endregion

    #region 共通ユーティリティ

    private async UniTask SafeRun(Func<UniTask> func)
    {
        try { await func(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Debug.LogError($"攻撃中にエラー: {ex}"); }
    }

    private void CancelAndDisposeCTS()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private void CancelAndDisposeComboReset()
    {
        if (_comboResetCts == null) return;
        _comboResetCts.Cancel();
        _comboResetCts.Dispose();
        _comboResetCts = null;
    }

    #endregion

    #region 外部呼出し

    /// <summary>
    /// 攻撃をキャンセル
    /// </summary>
    public void CancelAttack()
    {
        CancelAndDisposeCTS();
        Debug.Log("攻撃をキャンセルするメソッドが実行");
    }
    #endregion
}