using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private PlayerManager _manager;
    private InputHandler _input;
    private Animator _animator;

    [Header("Rigidbody")]
    [SerializeField] private Rigidbody _rb;

    public void Init(PlayerManager manager, InputHandler input, Animator animator)
    {
        _manager = manager;
        _input = input;
        _animator = animator;
    }

    private void Update()
    {
        Move();
        RotateToCameraForward();
        MoveAnimation();
    }

    private void Move()
    {
        Vector2 vel = _input.MoveInput;
        Camera camera = _manager.MainCamera;

        // 入力がゼロに近いなら移動なし
        float inputMag = vel.magnitude;
        if (inputMag < 0.01f)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 right = camera.transform.right * vel.x;
        Vector3 forward = camera.transform.forward * vel.y;

        // 方向だけ正規化
        Vector3 moveDir = (right + forward).normalized;
        moveDir.y = 0f;

        // 入力の大きさを速度に反映
        float speed = _manager.PlayerMoveSpeed * inputMag;

        _rb.linearVelocity = moveDir * speed;
    }


    private void RotateToCameraForward()
    {
        Camera camera = _manager.MainCamera;

        Vector3 targetDirection = camera.transform.forward;

        targetDirection.y = 0;

        transform.forward = targetDirection;
    }

    private void MoveAnimation()
    {
        Vector2 vel = _input.MoveInput;

        // BlendTreeに関する値
        _animator.SetFloat("MoveRight", vel.x);
        _animator.SetFloat("MoveForward", vel.y);

        // 遷移条件
        float speed = vel.magnitude;
        _animator.SetFloat("Speed", speed);
    }
}
