using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    const float INPUT_THRESHOLD = 0.001f;

    public event Action OnEndDodge;

    public void Init(PlayerStateManager playerStateManager,
        InputHandler input,
        CameraManager cameraManager,
        MoveData data,
        IStamina stamina,
        IModeController modeController,
        PlayerAnimationController animationController,
         AttackExecutor attackExecutor)
    {
        _playerStateManager = playerStateManager;
        _input = input;
        _cameraManager = cameraManager;
        _moveData = data;
        _stamina = stamina;
        _modeController = modeController;
        _animationController = animationController;

        _input.OnDodge += Dodge;

        _attackExecutor.OnAttackMoveRequested += HandleAttackMove;
    }

    [SerializeField] private Rigidbody _rb;

    private PlayerStateManager _playerStateManager;
    private InputHandler _input;
    private CameraManager _cameraManager;
    private MoveData _moveData;
    private IStamina _stamina;
    private IModeController _modeController;
    private PlayerAnimationController _animationController;
    private AttackExecutor _attackExecutor;

    private bool _canChainRoll;
    private float _chainTimer;

    // 攻撃時移動用
    private CancellationTokenSource _attackMoveCts;
    private bool _isAttackMoving;

    #region イベント関数

    private void Update()
    {
        // 攻撃時移動中は通常移動をスキップ
        if (!_isAttackMoving)
        {
            Move();
            Rotate();
        }

        PlayMoveAnimation();
        UpdateDodgeChain();
    }

    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnDodge -= Dodge;
        }

        if (_attackExecutor != null)
        {
            _attackExecutor.OnAttackMoveRequested -= HandleAttackMove;
        }

        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
    }

    #endregion

    /// <summary>
    /// 攻撃時の移動要求を処理
    /// </summary>
    private void HandleAttackMove(AttackMoveRequest request)
    {
        // 既存の攻撃移動をキャンセル
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
        _attackMoveCts = new CancellationTokenSource();

        PerformAttackMove(request).Forget();
    }

    /// <summary>
    /// 攻撃時移動の実行
    /// </summary>
    private async UniTaskVoid PerformAttackMove(AttackMoveRequest request)
    {
        _isAttackMoving = true;

        try
        {
            switch (request.MoveType)
            {
                case AttackMoveType.Dash:
                    await DashMove(request);
                    break;
                case AttackMoveType.Step:
                    await StepMove(request);
                    break;
                case AttackMoveType.Curve:
                    await CurveMove(request);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合
        }
        finally
        {
            _isAttackMoving = false;
            _rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 突進移動
    /// </summary>
    private async UniTask DashMove(AttackMoveRequest request)
    {
        float elapsed = 0f;
        Vector3 moveDir = request.Direction.normalized;
        moveDir.y = 0;

        while (elapsed < request.Duration)
        {
            _rb.linearVelocity = moveDir * request.Speed;

            elapsed += Time.deltaTime;
            await UniTask.Yield(_attackMoveCts.Token);
        }
    }

    /// <summary>
    /// ステップ移動（小移動）
    /// </summary>
    private async UniTask StepMove(AttackMoveRequest request)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + request.Direction.normalized * request.Distance;
        targetPos.y = startPos.y;

        while (elapsed < request.Duration)
        {
            float t = elapsed / request.Duration;
            // イージング（加速→減速）
            float smoothT = Mathf.SmoothStep(0, 1, t);

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, smoothT);
            _rb.linearVelocity = (newPos - transform.position) / Time.deltaTime;

            elapsed += Time.deltaTime;
            await UniTask.Yield(_attackMoveCts.Token);
        }
    }

    /// <summary>
    /// 曲線移動（将来的にホーミングなど）
    /// </summary>
    private async UniTask CurveMove(AttackMoveRequest request)
    {
        // 現時点では Dash と同じ実装
        await DashMove(request);
    }

    private void Move()
    {
        if (!_playerStateManager.CanMove()) { return; }

        var vec = _input.MoveInput;
        var camera = _cameraManager.MainCamera;

        var inputMag = vec.magnitude;

        if (inputMag < INPUT_THRESHOLD)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        var right = camera.transform.right * vec.x;
        var forward = camera.transform.forward * vec.y;

        var moveDir = (right + forward).normalized;
        moveDir.y = 0;

        var speed = _modeController.ModeData.MoveSpeed * inputMag;

        _rb.linearVelocity = moveDir * speed;
    }

    private void Rotate()
    {
        if (!_playerStateManager.CanMove()) { return; }

        var vec = _input.MoveInput;
        if (vec.magnitude < INPUT_THRESHOLD) { return; }

        var camera = _cameraManager.MainCamera;

        var right = camera.transform.right * vec.x;
        var forward = camera.transform.forward * vec.y;

        var lookDir = right + forward;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude <= 0f) { return; }

        var targetRotation = Quaternion.LookRotation(lookDir);

        float rotateSpeed = _moveData.RotateSpeed;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    private async UniTaskVoid DodgeInternal(DodgeType type)
    {
        if (!_playerStateManager.CanDodge()) { return; }

        float staminaCost =
            type == DodgeType.Step
            ? _stamina.GetDodgeStaminaCost()
            : _stamina.GetDodgeStaminaCost(); // 将来分けてもいい

        if (!_stamina.TryUseStamina(staminaCost)) { return; }

        var dodgeData =
            type == DodgeType.Step
            ? _moveData.StepDodge
            : _moveData.RollDodge;

        _playerStateManager.ChangeState(PlayerState.Dodge);
        PlayDodgeAnimation(type);

        Vector3 dodgeDir = GetDodgeDirection();

        float t = 0f;

        try
        {
            while (t < dodgeData.Duration)
            {
                _rb.linearVelocity = dodgeDir * dodgeData.Speed;
                t += Time.deltaTime;
                await UniTask.Yield(destroyCancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _rb.linearVelocity = Vector3.zero;

        OnDodgeEnd(type);
    }

    private void OnDodgeEnd(DodgeType type)
    {
        _playerStateManager.ChangeState(PlayerState.Idle);

        if (type == DodgeType.Step)
        {
            _canChainRoll = true;
            _chainTimer = _moveData.StepDodge.ChainWindow;
        }
        else
        {
            _canChainRoll = false;
        }

        OnEndDodge?.Invoke();
    }

    private Vector3 GetDodgeDirection()
    {
        var input = _input.MoveInput;
        if (input.magnitude > INPUT_THRESHOLD)
        {
            var camera = _cameraManager.MainCamera;
            var right = camera.transform.right * input.x;
            var forward = camera.transform.forward * input.y;
            var dir = right + forward;
            dir.y = 0f;
            return dir.normalized;
        }
        else
        {
            return transform.forward;
        }
    }

    private void PlayMoveAnimation()
    {
        if (_playerStateManager.IsDodging()) { return; }

        if (_animationController != null)
        {
            var speed = _rb.linearVelocity.magnitude;

            _animationController.UpdateMoveAnimation(speed);
        }
    }

    private void PlayDodgeAnimation(DodgeType type)
    {
        if (type == DodgeType.Step)
        {
            _animationController.PlayStepDodge();
        }
        else
        {
            _animationController.PlayRollDodge();
        }
    }

    private void UpdateDodgeChain()
    {
        if (!_canChainRoll) { return; }

        _chainTimer -= Time.deltaTime;
        if (_chainTimer <= 0f)
        {
            _canChainRoll = false;
        }
    }

    // 匿名関数回避のためのメソッド
    private void Dodge()
    {
        if (_canChainRoll)
        {
            DodgeInternal(DodgeType.Roll).Forget();
        }
        else
        {
            DodgeInternal(DodgeType.Step).Forget();
        }
    }

}
