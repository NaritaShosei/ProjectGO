using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private PlayerInput _input;

    public Vector2 MoveInput { get; private set; }
    public event Action OnLightAttack;
    public event Action OnHeavyAttack;
    public event Action OnChargeAttack;

    private void OnEnable()
    {
        _input = new PlayerInput();

        _input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _input.Player.Move.canceled += _ => MoveInput = Vector2.zero;

        _input.Player.LightAttack.performed += _ => OnLightAttack?.Invoke();
        _input.Player.HeavyAttack.performed += _ => OnHeavyAttack?.Invoke();

        _input.Player.ChargeAttack.performed += _ => OnChargeAttack?.Invoke();

        _input.Enable();
    }
}
