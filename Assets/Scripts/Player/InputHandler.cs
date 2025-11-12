using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private PlayerInput _input;

    public Vector2 MoveInput { get; private set; }

    private void OnEnable()
    {
        _input = new PlayerInput();

        _input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _input.Player.Move.canceled += _ => MoveInput = Vector2.zero;

        _input.Enable();
    }
}
