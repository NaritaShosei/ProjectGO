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
        MoveAnimation();
    }

    private void Move()
    {
        Vector2 vel = _input.MoveInput;

        Vector3 right = _manager.MainCamera.transform.right * vel.x;
        Vector3 forward = _manager.MainCamera.transform.forward * vel.y;

        Vector3 moveDir = (right + forward).normalized;
        moveDir.y = 0;

        _rb.linearVelocity = moveDir * _manager.PlayerMoveSpeed;
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
