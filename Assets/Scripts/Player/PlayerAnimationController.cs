using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public void Init(PlayerStateManager stateManager, IModeController modeController)
    {
        _stateManager = stateManager;
        _modeController = modeController;

        _stateManager.OnStateChanged += OnStateChanged;
        _modeController.OnModeChanged += OnModeChanged;
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

    public void PlayDodge()
    {
        _animator.SetTrigger(AnimParams.Dodge);
    }

    public void OnDestroy()
    {
        if (_stateManager != null)
            _stateManager.OnStateChanged -= OnStateChanged;
        if (_modeController != null)
            _modeController.OnModeChanged -= OnModeChanged;
    }

    [SerializeField] private Animator _animator;
    private int _baseLayer;
    private int _bodyLayer;


    // アニメーションパラメータ名（定数化）
    private static class AnimParams
    {
        public const string Base = "Base Layer";
        public const string Body = "BodyUpper";

        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int AttackId = Animator.StringToHash("AttackId");
        public static readonly int Dodge = Animator.StringToHash("Dodge");
        public static readonly int IsCharging = Animator.StringToHash("IsCharging");
        public static readonly int Damaged = Animator.StringToHash("Damaged");
        public static readonly int Dead = Animator.StringToHash("Dead");
        public static readonly int PlayerMode = Animator.StringToHash("PlayerMode");
    }

    private void Awake()
    {
        _baseLayer = _animator.GetLayerIndex(AnimParams.Base);
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
}