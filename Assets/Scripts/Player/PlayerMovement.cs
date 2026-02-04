using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    const float INPUT_THRESHOLD = 0.001f;

    public event Action OnEndDodge;

    public void Init(PlayerStateManager playerStateManager,
        InputHandler input,
        CameraManager cameraManager,
        MoveData data,
        IStamina stamina,
        IModeController modeController,
        PlayerAnimationController animationController)
    {
        _playerStateManager = playerStateManager;
        _input = input;
        _cameraManager = cameraManager;
        _moveData = data;
        _stamina = stamina;
        _modeController = modeController;
        _animationController = animationController;

        _input.OnDodge += Dodge;
    }

    [SerializeField] private Rigidbody _rb;

    private PlayerStateManager _playerStateManager;
    private InputHandler _input;
    private CameraManager _cameraManager;
    private MoveData _moveData;
    private IStamina _stamina;
    private IModeController _modeController;
    private PlayerAnimationController _animationController;

    #region イベント関数

    private void Update()
    {
        Move();
        Rotate();
        PlayMoveAnimation();
    }

    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnDodge -= Dodge;
        }
    }

    #endregion

    private void Move()
    {
        if (!_playerStateManager.CanMove()) { return; }

        var vec = _input.MoveInput;
        var camera = _cameraManager.MainCamera;

        var inputMag = vec.magnitude;

        if (inputMag < INPUT_THRESHOLD)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        var right = camera.transform.right * vec.x;
        var forward = camera.transform.forward * vec.y;

        var moveDir = (right + forward).normalized;
        moveDir.y = 0;

        var speed = _modeController.ModeData.MoveSpeed * inputMag;

        _rb.linearVelocity = moveDir * speed;
    }

    private void Rotate()
    {
        if (!_playerStateManager.CanMove()) { return; }

        var vec = _input.MoveInput;
        if (vec.magnitude < INPUT_THRESHOLD) { return; }

        var camera = _cameraManager.MainCamera;

        var right = camera.transform.right * vec.x;
        var forward = camera.transform.forward * vec.y;

        var lookDir = right + forward;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude <= 0f) { return; }

        var targetRotation = Quaternion.LookRotation(lookDir);

        float rotateSpeed = _moveData.RotateSpeed;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    private async UniTaskVoid OnDodge()
    {
        if (!_playerStateManager.CanDodge()) return;
        if (!_stamina.TryUseStamina(_stamina.GetDodgeStaminaCost())) return;

        _playerStateManager.ChangeState(PlayerState.Dodge);
        _animationController.PlayDodge();

        Vector3 dodgeDir = GetDodgeDirection();

        float t = 0f;

        try
        {
            while (t < _moveData.DodgeDuration)
            {
                _rb.linearVelocity = dodgeDir * _moveData.DodgeSpeed;
                t += Time.deltaTime;
                await UniTask.Yield(destroyCancellationToken);
            }
        }
        catch (OperationCanceledException) { }

        _rb.linearVelocity = Vector3.zero;
        _playerStateManager.ChangeState(PlayerState.Idle);
        OnEndDodge?.Invoke();
    }

    private Vector3 GetDodgeDirection()
    {
        var input = _input.MoveInput;
        if (input.magnitude > INPUT_THRESHOLD)
        {
            var camera = _cameraManager.MainCamera;
            var right = camera.transform.right * input.x;
            var forward = camera.transform.forward * input.y;
            var dir = right + forward;
            dir.y = 0f;
            return dir.normalized;
        }
        else
        {
            return transform.forward;
        }
    }

    private void PlayMoveAnimation()
    {
        if (_playerStateManager.IsDodging()) { return; }

        if (_animationController != null)
        {
            var speed = _rb.linearVelocity.magnitude;

            _animationController.UpdateMoveAnimation(speed);
        }
    }

    // 匿名関数回避のためのメソッド
    private void Dodge()
    {
        OnDodge().Forget();
    }
}
