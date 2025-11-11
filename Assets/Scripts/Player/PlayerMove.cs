using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private PlayerManager _manager;
    private InputHandler _input;

    [SerializeField] private Rigidbody _rb;

    public void Init(PlayerManager manager, InputHandler input)
    {
        _manager = manager;
        _input = input;
    }

    private void Update()
    {
        
    }

    private void Move()
    {
        Vector2 vel = _input.MoveInput;
    }
}
