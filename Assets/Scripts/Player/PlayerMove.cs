using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float INPUT_THRESHOLD = 0.001f;

    private PlayerManager _manager;
    private InputHandler _input;
    private Animator _animator;

    [Header("Rigidbody")]
    [SerializeField] private Rigidbody _rb;

    [Header("回転方向への補間率 (0〜1)")]
    [SerializeField, Range(0, 1)] private float _rotateSmooth = 0.5f;

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
        if (inputMag < INPUT_THRESHOLD)
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
        Vector2 vel = _input.MoveInput;

        // 入力がゼロに近いときは回転しない
        if (vel.magnitude < INPUT_THRESHOLD) { return; }

        Camera camera = _manager.MainCamera;

        // 前後左右の移動すべて正面を向くようにするためカメラの正面のみ考慮
        Vector3 camForward = camera.transform.forward;

        camForward.y = 0;

        // 線形補間で滑らかに回転
        var dir = Vector3.Lerp(transform.forward, camForward.normalized, _rotateSmooth);

        transform.rotation = Quaternion.LookRotation(dir);
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
