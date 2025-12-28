using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public event Action OnEndDodge;

    public void Init(PlayerStateManager playerStateManager,
        InputHandler input,
        CameraManager cameraManager,
        MoveData data,
        IStamina stamina)
    {
        _playerStateManager = playerStateManager;
        _input = input;
        _cameraManager = cameraManager;
        _moveData = data;
        _stamina = stamina;

        _input.OnDodge += OnDodge;
    }

    const float INPUT_THRESHOLD = 0.001f;

    [SerializeField] private Rigidbody _rb;

    private PlayerStateManager _playerStateManager;
    private InputHandler _input;
    private CameraManager _cameraManager;
    private MoveData _moveData;
    private IStamina _stamina;

    #region イベント関数

    private void Update()
    {
        Move();
        Rotate();
    }

    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnDodge -= OnDodge;
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

        var speed = _moveData.MoveSpeed * inputMag;

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

    private async void OnDodge()
    {
        if (!_playerStateManager.CanDodge()) { return; }

        if (!_stamina.TryUseStamina(_stamina.GetDodgeStaminaCost()))
        {
            return; // スタミナ不足で回避不可
        }

        _playerStateManager.ChangeState(PlayerState.Dodge);

        var input = _input.MoveInput;
        Vector3 dodgeDir;

        if (input.magnitude > INPUT_THRESHOLD)
        {
            var camera = _cameraManager.MainCamera;
            var right = camera.transform.right * input.x;
            var forward = camera.transform.forward * input.y;
            dodgeDir = (right + forward).normalized;
        }
        else
        {
            dodgeDir = transform.forward;
        }

        dodgeDir.y = 0f;

        float t = 0;

        try
        {
            while (t < _moveData.DodgeDuration)
            {
                _rb.linearVelocity = _moveData.DodgeSpeed * dodgeDir;

                t += Time.deltaTime;
                await UniTask.Yield(destroyCancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常終了
        }
        _playerStateManager.ChangeState(PlayerState.Idle);

        OnEndDodge?.Invoke();
    }
}
