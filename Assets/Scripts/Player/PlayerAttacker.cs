using Cysharp.Threading.Tasks;
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

    [Header("Animator")]
    [SerializeField] private Animator _animator;

    private AttackData _currentCombData;
    private bool _isAttacking;
    private bool _isCharging;
    private float _chargeTimer;

    private CancellationTokenSource _cts;

    public void Init(PlayerManager manager, InputHandler input)
    {
        _manager = manager;
        _input = input;

        _input.OnLightAttack += HandleComboAttack;

        _currentCombData = _firstComboData;
    }

    #region 弱攻撃コンボ

    private async void HandleComboAttack()
    {
        if (_isAttacking) { return; }
        _cts = new CancellationTokenSource();

        await PerformComboAttack(_currentCombData, _cts.Token);
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
        _currentCombData = attack.NextCombo != null ? attack.NextCombo : _firstComboData;

        _isAttacking = false;
    }
    #endregion
}
