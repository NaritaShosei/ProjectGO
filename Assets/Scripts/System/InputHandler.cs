using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    // イベント
    public event Action OnDodge;
    public event Action OnLightAttack;
    public event Action OnChargeStart;
    public event Action OnChargeEnd;
    public event Action OnInteract;
    public event Action OnModeChange;

    /// <summary>
    /// PlayerのActionMapの有効か非有効化の切り替え。
    /// </summary>
    /// <param name="enable">trueで有効化</param>
    public void EnableInput(bool enable)
    {
        if (enable)
        {
            _input.Player.Enable();
        }
        else
        {
            _input.Player.Disable();
        }
    }

    private PlayerInput _input;

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void OnEnable()
    {
        _input = new PlayerInput();

        // 移動
        _input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _input.Player.Move.canceled += _ => MoveInput = Vector2.zero;

        // 回避
        _input.Player.Dodge.started += _ => OnDodge?.Invoke();

        // 弱攻撃 
        _input.Player.LightAttack.performed += _ => OnLightAttack?.Invoke();

        // 強攻撃
        _input.Player.ChargeAttack.started += _ => OnChargeStart?.Invoke();
        _input.Player.ChargeAttack.canceled += _ => OnChargeEnd?.Invoke();

        // インタラクト
        _input.Player.Interact.started += _ => OnInteract?.Invoke();

        // モードチェンジ
        _input.Player.ModeChange.started += _ => OnModeChange?.Invoke();

        EnableInput(true);
    }

    private void OnDisable()
    {
        EnableInput(false);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<InputHandler>())
        {
            ServiceLocator.Unregister<InputHandler>();
        }
    }
}