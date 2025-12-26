using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public void Init(PlayerStateManager playerStateManager,
        InputHandler input,
        CameraManager cameraManager,
        MoveData data)
    {
        _playerStateManager = playerStateManager;
        _input = input;
        _cameraManager = cameraManager;
        _moveData = data;
    }

    const float INPUT_THRESHOLD = 0.001f;

    [SerializeField] private Rigidbody _rb;

    private PlayerStateManager _playerStateManager;
    private InputHandler _input;
    private CameraManager _cameraManager;
    private MoveData _moveData;

    private void Update()
    {
        Move();
        Rotate();
    }

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
}
