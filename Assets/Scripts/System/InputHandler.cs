using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 CameraMoveInput { get; private set; }

    // イベント
    public event Action OnDodge;
    public event Action OnLightAttackPressed;
    public event Action OnLightAttackReleased;
    public event Action OnInteract;
    public event Action OnModeChange;
    public event Action OnLockOn;
    public event Action OnLockOnLeft;
    public event Action OnLockOnRight;

    /// <summary>
    /// PlayerのActionMapの有効か非有効化の切り替え。
    /// </summary>
    /// <param name="enable">trueで有効化</param>

    public void EnableInput(bool enable)
    {
        if (enable)
        {
            _isDisablingInput = false;
            _input.Player.Enable();
        }
        else
        {
            _isDisablingInput = true;
            MoveInput = Vector2.zero;
            CameraMoveInput = Vector2.zero;
            _input.Player.Disable();
        }
    }

    private PlayerInput _input;
    private bool _isDisablingInput;

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

        _input.Player.CameraMove.performed += OnCameraMovePerformed;
        _input.Player.CameraMove.canceled += OnCameraMoveCanceled;

        // 回避
        _input.Player.Dodge.started += _ => OnDodge?.Invoke();

        // 弱攻撃 
        _input.Player.LightAttack.started += _ => OnLightAttackPressed?.Invoke();
        // 弱攻撃のキャンセル（ボタンを離したとき）
        _input.Player.LightAttack.canceled += _ =>
        {
            if (_isDisablingInput) return;

            OnLightAttackReleased?.Invoke();
        };

        // インタラクト
        _input.Player.Interact.started += _ => OnInteract?.Invoke();

        // モードチェンジ
        _input.Player.ModeChange.started += _ => OnModeChange?.Invoke();

        // ロックオン
        _input.Player.LockOn.started += _ => OnLockOn?.Invoke();

        // ロックオン切り替え
        _input.Player.LockOnChange.performed += ctx =>
        {
            Vector2 value = ctx.ReadValue<Vector2>();
            if (value.x < 0f) OnLockOnLeft?.Invoke();
            else if (value.x > 0f) OnLockOnRight?.Invoke();
        };

        EnableInput(true);
    }

    private void OnDisable()
    {
        EnableInput(false);

        _input.Player.CameraMove.performed -= OnCameraMovePerformed;
        _input.Player.CameraMove.canceled -= OnCameraMoveCanceled;
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<InputHandler>())
        {
            ServiceLocator.Unregister<InputHandler>();
        }
    }

    private void OnCameraMovePerformed(InputAction.CallbackContext context)
    {
        CameraMoveInput = context.ReadValue<Vector2>();
    }

    private void OnCameraMoveCanceled(InputAction.CallbackContext _)
    {
        CameraMoveInput = Vector2.zero;
    }
}
