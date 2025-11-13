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
        if (_isAttacking) { return; }

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
        await UniTask.Delay((int)(attack.MotionDuration * 1000), false, PlayerLoopTiming.Update, token);

        // コンボ継続可能なら次段へ
        _currentComboData = attack.NextCombo != null ? attack.NextCombo : _firstComboData;

        _isAttacking = false;
    }
    #endregion

    #region 溜め攻撃

    private async void StartCharge()
    {
        if (_isAttacking) return;
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

    }

    private async UniTask PerformHeavyAttack(AttackData data, CancellationToken token)
    {
        _isAttacking = true;

        if (data.AnimationClip && _animator)
            _animator.Play(data.AnimationClip.name);

        await UniTask.Delay((int)(data.MotionDuration * 1000), false, PlayerLoopTiming.Update, token);

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

}
