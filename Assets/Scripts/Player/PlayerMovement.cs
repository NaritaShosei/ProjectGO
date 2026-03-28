using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    const float INPUT_THRESHOLD = 0.001f;

    public event Action OnEndDodge;

    public void Init(
        PlayerStateManager playerStateManager,
        InputHandler input,
        MoveData data,
        IModeController modeController,
        PlayerAnimationController animationController,
        PlayerAttack attack)
    {
        _playerStateManager = playerStateManager;
        _input = input;
        _moveData = data;
        _modeController = modeController;
        _animationController = animationController;
        _attack = attack;

        _input.OnDodge += Dodge;
        _attack.OnAttackMoveRequested += HandleAttackMove;

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            _cameraManager = cameraManager;
        }
        else
        {
            Debug.LogError($"[{this}]:CameraManagerが見つかりませんでした。");
        }
    }

    public void SetTimeScale(float scale)
    {
        _timeScale = scale;
    }

    [SerializeField] private Rigidbody _rb;

    private PlayerStateManager _playerStateManager;
    private InputHandler _input;
    private CameraManager _cameraManager;
    private MoveData _moveData;
    private IModeController _modeController;
    private PlayerAnimationController _animationController;
    private PlayerAttack _attack;

    private float _timeScale = 1;

    private bool _canChainRoll;
    private float _chainTimer;

    private CancellationTokenSource _attackMoveCts;
    private bool _isAttackMoving;
    private bool _currentIsPhantom;

    #region イベント関数

    private void Update()
    {
        if (!_isAttackMoving)
        {
            Rotate();
            PlayMoveAnimation();
        }

        UpdateDodgeChain();
    }

    private void FixedUpdate()
    {
        if (!_isAttackMoving)
        {
            Move();
        }
    }

    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnDodge -= Dodge;
        }

        if (_attack != null)
        {
            _attack.OnAttackMoveRequested -= HandleAttackMove;
        }

        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
    }

    #endregion

    private void HandleAttackMove(AttackMoveRequest request)
    {
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
        _attackMoveCts = new CancellationTokenSource();

        if (_currentIsPhantom)
        {
            Physics.IgnoreLayerCollision(
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Enemy"),
                false
            );
            _currentIsPhantom = false;
        }

        PerformAttackMove(request).Forget();
    }

    private async UniTaskVoid PerformAttackMove(AttackMoveRequest request)
    {
        _isAttackMoving = true;

        if (request.IsPhantom)
        {
            _currentIsPhantom = true;
            Physics.IgnoreLayerCollision(
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Enemy"),
                true
            );
        }

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
        catch (OperationCanceledException) { }
        finally
        {
            _isAttackMoving = false;

            if (_rb)
            {
                _rb.linearVelocity = Vector3.zero;
            }

            if (_currentIsPhantom)
            {
                Physics.IgnoreLayerCollision(
                    LayerMask.NameToLayer("Player"),
                    LayerMask.NameToLayer("Enemy"),
                    false
                );
                _currentIsPhantom = false;
            }
        }
    }

    private async UniTask DashMove(AttackMoveRequest request)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * request.Distance;
        targetPos.y = startPos.y;

        while (true)
        {
            if (request.Duration <= 0f) { return; }
            if (elapsed >= request.Duration) { break; }
            if (request.Target &&
                Vector3.Distance(request.Target.position, transform.position) < request.StopDistance) { break; }

            float t = elapsed / request.Duration * _timeScale;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, smoothT);
            _rb.MovePosition(newPos);

            elapsed += Time.fixedDeltaTime * _timeScale;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, _attackMoveCts.Token);
        }
    }

    private async UniTask StepMove(AttackMoveRequest request)
    {
        float elapsed = 0f;
        Vector3 moveDir = transform.forward;
        moveDir.y = 0;

        float speed = request.Distance / request.Duration;

        while (true)
        {
            if (request.Duration <= 0f) { return; }
            if (elapsed >= request.Duration) { break; }
            if (request.Target &&
                Vector3.Distance(request.Target.position, transform.position) < request.StopDistance) { break; }

            _rb.linearVelocity = moveDir * speed * _timeScale;

            elapsed += Time.fixedDeltaTime * _timeScale;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, _attackMoveCts.Token);
        }
    }

    private async UniTask CurveMove(AttackMoveRequest request)
    {
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
        _rb.linearVelocity = moveDir * speed * _timeScale;
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
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _moveData.RotateSpeed * Time.deltaTime * _timeScale
        );
    }

    /// <summary>
    /// スタミナチェックなしで回避を実行する。
    /// </summary>
    private async UniTaskVoid DodgeInternal(DodgeType type)
    {
        if (!_playerStateManager.CanDodge()) { return; }

        var dodgeData = type == DodgeType.Step
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
                _rb.linearVelocity = dodgeDir * dodgeData.Speed * _timeScale;
                t += Time.deltaTime * _timeScale;
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

        return transform.forward;
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
            _animationController.PlayStepDodge();
        else
            _animationController.PlayRollDodge();
    }

    private void UpdateDodgeChain()
    {
        if (!_canChainRoll) { return; }

        _chainTimer -= Time.deltaTime * _timeScale;
        if (_chainTimer <= 0f)
        {
            _canChainRoll = false;
        }
    }

    private void Dodge()
    {
        if (_canChainRoll)
            DodgeInternal(DodgeType.Roll).Forget();
        else
            DodgeInternal(DodgeType.Step).Forget();
    }
}
