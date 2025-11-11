using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private PlayerInput _input = new();

    public Vector2 MoveInput { get; private set; }

    private void OnEnable()
    {
        _input.Player.Move.performed += cts => MoveInput = cts.ReadValue<Vector2>();
        _input.Player.Move.canceled += cts => MoveInput = Vector2.zero;

        _input.Enable();
    }
}
