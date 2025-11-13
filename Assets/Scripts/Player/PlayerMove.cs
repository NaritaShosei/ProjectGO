using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private PlayerManager _manager;
    private InputHandler _input;

    [Header("Rigidbody")]
    [SerializeField] private Rigidbody _rb;

    public void Init(PlayerManager manager, InputHandler input)
    {
        _manager = manager;
        _input = input;
    }

    private void Update()
    {
        Move();
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
}
