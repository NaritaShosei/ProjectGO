using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private PlayerInput _inputActions;

    private void Awake()
    {
        InitializeAction();
    }

    private void InitializeAction()
    {
        _inputActions = new PlayerInput();

        _inputActions.Enable();

        _inputActions.Player.Move.performed += (context) => Move();
    }

    private void Move()
    {
        Debug.Log("Test");
    }
}
