using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private PlayerInput _input;

    public Vector2 MoveInput { get; private set; }

    public event Action OnDodge;
    public event Action OnLightAttack;
    public event Action OnChargeStart;
    public event Action OnChargeEnd;
    public event Action OnInteract;
    public event Action OnModeChange;

    private void OnEnable()
    {
        _input = new PlayerInput();

        _input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _input.Player.Move.canceled += _ => MoveInput = Vector2.zero;

        _input.Player.Dodge.started += _ => OnDodge?.Invoke();

        _input.Player.LightAttack.performed += _ => OnLightAttack?.Invoke();

        _input.Player.ChargeAttack.started += _ => OnChargeStart?.Invoke();
        _input.Player.ChargeAttack.canceled += _ => OnChargeEnd?.Invoke();

        _input.Player.Interact.started += _ => OnInteract?.Invoke();

        _input.Player.ModeChange.started += _ => OnModeChange?.Invoke();

        _input.Enable();
    }
}
