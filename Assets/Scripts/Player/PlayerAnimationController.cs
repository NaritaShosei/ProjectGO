using System;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour,IAnimationController
{
    public void Init(PlayerStateManager stateManager, IModeController modeController)
    {
        _stateManager = stateManager;
        _modeController = modeController;

        _stateManager.OnStateChanged += OnStateChanged;
        _modeController.OnModeChanged += OnModeChanged;
    }

    public event Action OnAttackComplete;
    public event Action OnComboWindowStart;
    public event Action OnComboWindowEnd;
    public event Action OnAttackExecute;

    // アニメーションから呼ばれる関数
    public void AnimEvent_AttackExecute()
    {
        OnAttackExecute?.Invoke();
    }

    public void AnimEvent_AttackComplete()
    {
        OnAttackComplete?.Invoke();
    }

    public void AnimEvent_ComboWindowStart()
    {
        OnComboWindowStart?.Invoke();
    }

    public void AnimEvent_ComboWindowEnd()
    {
        OnComboWindowEnd?.Invoke();
    }

    public void UpdateMoveAnimation(float speed)
    {
        _animator.SetFloat(AnimParams.Speed, speed);
    }

    public void PlayAttack(int attackId)
    {
        _animator.SetInteger(AnimParams.AttackId, attackId);
        _animator.SetTrigger(AnimParams.Attack);
    }

    public void PlayStepDodge()
    {
        _animator.SetTrigger(AnimParams.Step);
    }

    public void PlayRollDodge()
    {
        _animator.SetTrigger(AnimParams.Roll);
    }

    public void SetAnimSpeed(float speed)
    {
        // 1 のときは元の速度に戻す
        if (speed == 1f)
        {
            // animator.speed = 1f; とせずに、変更前の速度を保存しておいてそこに戻す
            Debug.Log("[Animator]animator.speed = 1f; とせずに、変更前の速度を保存しておいてそこに戻す");
            _animator.speed = _beforeAnimSpeed;
            _isSpeedChanging = false;
            return;
        }

        if (_isSpeedChanging)
        {
            Debug.LogWarning($"[Animator]現在速度変更中のため、SetAnimSpeedの呼び出しを無視します (requested: {speed})");
            return;
        }

        // それ以外のときは現在の速度を保存して倍率適用
        _beforeAnimSpeed = _animator.speed;
        _animator.speed *= speed;
        _isSpeedChanging = true;
        Debug.Log($"[Animator]SetAnimSpeed: {_animator.speed} (before: {_beforeAnimSpeed}, multiplier: {speed})");
    }

    public void OnDestroy()
    {
        if (_stateManager != null)
            _stateManager.OnStateChanged -= OnStateChanged;
        if (_modeController != null)
            _modeController.OnModeChanged -= OnModeChanged;
    }

    [SerializeField] private Animator _animator;
    private int _bodyLayer;


    // アニメーションパラメータ名（定数化）
    private static class AnimParams
    {
        public const string Body = "BodyUpper";

        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int AttackId = Animator.StringToHash("AttackId");
        public static readonly int Step = Animator.StringToHash("Step");
        public static readonly int Roll = Animator.StringToHash("Roll");
        public static readonly int IsCharging = Animator.StringToHash("IsCharging");
        public static readonly int Damaged = Animator.StringToHash("Damaged");
        public static readonly int Dead = Animator.StringToHash("Dead");
        public static readonly int PlayerMode = Animator.StringToHash("PlayerMode");
    }

    private void Awake()
    {
        _bodyLayer = _animator.GetLayerIndex(AnimParams.Body);
    }

    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Charging:
                _animator.SetBool(AnimParams.IsCharging, true);
                _animator.SetLayerWeight(_bodyLayer, 1);
                break;
            case PlayerState.Dead:
                _animator.SetTrigger(AnimParams.Dead);
                break;
            case PlayerState.Damaged:
                _animator.SetTrigger(AnimParams.Damaged);
                break;
        }

        if (oldState == PlayerState.Charging)
        {
            _animator.SetBool(AnimParams.IsCharging, false);
            _animator.SetLayerWeight(_bodyLayer, 0);
        }
    }

    private void OnModeChanged(PlayerMode newMode)
    {
        _animator.SetInteger(AnimParams.PlayerMode, (int)newMode);
    }

    private PlayerStateManager _stateManager;
    private IModeController _modeController;

    private float _beforeAnimSpeed = 1f;
    private bool _isSpeedChanging = false;
}
