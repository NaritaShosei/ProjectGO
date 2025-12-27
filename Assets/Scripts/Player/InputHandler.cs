using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private PlayerInput _input;

    public Vector2 MoveInput { get; private set; }

    // ボタン押下状態 
    public bool IsLightAttackHeld { get; private set; }
    public bool IsChargeAttackHeld { get; private set; }

    // イベント
    public event Action OnDodge;
    public event Action OnLightAttack;
    public event Action OnChargeStart;
    public event Action OnChargeEnd;
    public event Action OnInteract;
    public event Action OnModeChange;

    private void OnEnable()
    {
        _input = new PlayerInput();

        // 移動
        _input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _input.Player.Move.canceled += _ => MoveInput = Vector2.zero;

        // 回避
        _input.Player.Dodge.started += _ => OnDodge?.Invoke();

        // 弱攻撃 (押下状態も記録)
        _input.Player.LightAttack.started += _ => IsLightAttackHeld = true;
        _input.Player.LightAttack.performed += _ => OnLightAttack?.Invoke();
        _input.Player.LightAttack.canceled += _ => IsLightAttackHeld = false;

        // 強攻撃 (押下状態も記録)
        _input.Player.ChargeAttack.started += ctx =>
        {
            IsChargeAttackHeld = true;
            OnChargeStart?.Invoke();
        };
        _input.Player.ChargeAttack.canceled += ctx =>
        {
            IsChargeAttackHeld = false;
            OnChargeEnd?.Invoke();
        };

        // インタラクト
        _input.Player.Interact.started += _ => OnInteract?.Invoke();

        // モードチェンジ
        _input.Player.ModeChange.started += _ => OnModeChange?.Invoke();

        _input.Enable();
    }

    private void OnDisable()
    {
        _input?.Disable();
    }
}