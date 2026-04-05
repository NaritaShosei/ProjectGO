using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    const float INPUT_THRESHOLD = 0.001f;

    /// <summary>回避終了通知（PlayerAttackが購読してDodgeAttack判定に使う）</summary>
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

        // 被弾アニメーション終了をAnimationControllerから受け取る
        _animationController.OnDamagedEnd += HandleDamagedEnd;

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
            _cameraManager = cameraManager;
        else
            Debug.LogError($"[{this}]: CameraManagerが見つかりませんでした。");
    }

    public void SetTimeScale(float scale) => _timeScale = scale;

    /// <summary>ロックオン対象の設定（nullで解除）</summary>
    public void SetLockOnTarget(Transform target)
    {
        _lockOnTarget = target;
    }

    [SerializeField] private Rigidbody _rb;

    private PlayerStateManager _playerStateManager;
    private InputHandler _input;
    private CameraManager _cameraManager;
    private MoveData _moveData;
    private IModeController _modeController;
    private PlayerAnimationController _animationController;
    private PlayerAttack _attack;
    private Transform _lockOnTarget;

    private float _timeScale = 1f;
    private bool _isDodging;

    private CancellationTokenSource _attackMoveCts;
    private bool _isAttackMoving;
    private bool _currentIsPhantom;

    // ── Unity イベント ───────────────────────────────────────

    private void Update()
    {
        if (!_isAttackMoving)
        {
            Rotate();
            PlayMoveAnimation();
        }
    }

    private void FixedUpdate()
    {
        if (!_isAttackMoving)
            Move();
    }

    private void OnDestroy()
    {
        if (_input != null) _input.OnDodge -= Dodge;
        if (_attack != null) _attack.OnAttackMoveRequested -= HandleAttackMove;
        if (_animationController != null) _animationController.OnDamagedEnd -= HandleDamagedEnd;

        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
    }

    // ── 移動 ─────────────────────────────────────────────────

    private void Move()
    {
        if (!_playerStateManager.CanMove()) { _rb.linearVelocity = Vector3.zero; return; }

        var vec = _input.MoveInput;
        var camera = _cameraManager.MainCamera;
        float inputMag = vec.magnitude;

        if (inputMag < INPUT_THRESHOLD) { _rb.linearVelocity = Vector3.zero; return; }

        Vector3 moveDir;

        if (_lockOnTarget != null)
        {
            // ロックオン中: カメラ空間入力をそのままワールド方向に変換
            var right = camera.transform.right * vec.x;
            var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized * vec.y;
            moveDir = (right + forward).normalized;
        }
        else
        {
            var right = camera.transform.right * vec.x;
            var forward = camera.transform.forward * vec.y;
            moveDir = (right + forward).normalized;
        }
        moveDir.y = 0f;

        float speed = _modeController.ModeData.MoveSpeed * inputMag;
        _rb.linearVelocity = moveDir * speed * _timeScale;
    }

    private void Rotate()
    {
        if (!_playerStateManager.CanMove()) return;

        if (_lockOnTarget != null)
        {
            // ロックオン中: 常にターゲットの方向を向く
            Vector3 toTarget = _lockOnTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot,
                    _moveData.RotateSpeed * Time.deltaTime * _timeScale);
            }
            return;
        }

        // 通常: 移動方向を向く
        var vec = _input.MoveInput;
        if (vec.magnitude < INPUT_THRESHOLD) return;

        var camera = _cameraManager.MainCamera;
        var right = camera.transform.right * vec.x;
        var forward = camera.transform.forward * vec.y;
        var lookDir = right + forward;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude <= 0f) return;

        var targetRotation = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation,
            _moveData.RotateSpeed * Time.deltaTime * _timeScale);
    }

    private void PlayMoveAnimation()
    {
        if (_playerStateManager.IsDodging()) return;

        float speed = _rb.linearVelocity.magnitude;

        if (_lockOnTarget != null)
        {
            // 8方向ブレンド
            _animationController.UpdateLockedMoveAnimation(
                _input.MoveInput,
                transform.forward,
                _cameraManager.MainCamera.transform.right);
        }
        else
        {
            _animationController.UpdateMoveAnimation(speed);
        }
    }

    // ── 回避 ─────────────────────────────────────────────────

    private void Dodge()
    {
        if (!_playerStateManager.CanDodge()) return;

        // 攻撃中なら中断してリセット
        if (_playerStateManager.CurrentState == PlayerState.Attacking)
        {
            _attack.InterruptByDodge();
        }

        DodgeAsync().Forget();
    }

    private async UniTaskVoid DodgeAsync()
    {
        if (_isDodging) return;

        _isDodging = true;
        _playerStateManager.ChangeState(PlayerState.Dodge);

        // モード対応の回避データ取得
        DodgeData dodgeData = _moveData.GetDodge(_modeController.CurrentMode);
        Vector3 dodgeDir = GetDodgeDirection();

        // アニメーション再生
        if (_lockOnTarget != null)
        {
            // ロックオン中: ローカル方向を計算してBlendTree用パラメータをセット
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            float lx = Vector3.Dot(dodgeDir, right);
            float ly = Vector3.Dot(dodgeDir, fwd);
            _animationController.PlayLockedDodge(lx, ly, dodgeData.AnimationStateName);
        }
        else
        {
            _animationController.PlayDodge(dodgeData.AnimationStateName);
        }

        float elapsed = 0f;
        try
        {
            while (elapsed < dodgeData.Duration)
            {
                _rb.linearVelocity = dodgeDir * dodgeData.Speed * _timeScale;
                elapsed += Time.deltaTime * _timeScale;
                await UniTask.Yield(destroyCancellationToken);
            }
        }
        catch (OperationCanceledException) { return; }

        _rb.linearVelocity = Vector3.zero;
        _isDodging = false;
        _playerStateManager.ChangeState(PlayerState.Idle);
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

    // ── 被弾 ─────────────────────────────────────────────────

    private void HandleDamagedEnd()
    {
        // 被弾アニメーション終了でIdle復帰
        if (_playerStateManager.IsDamaged())
            _playerStateManager.ChangeState(PlayerState.Idle);
    }

    // ── 攻撃移動 ─────────────────────────────────────────────

    private void HandleAttackMove(AttackMoveRequest request)
    {
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
        _attackMoveCts = new CancellationTokenSource();

        if (_currentIsPhantom)
        {
            Physics.IgnoreLayerCollision(
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Enemy"), false);
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
                LayerMask.NameToLayer("Enemy"), true);
        }

        try
        {
            switch (request.MoveType)
            {
                case AttackMoveType.Dash: await DashMove(request); break;
                case AttackMoveType.Step: await StepMove(request); break;
                case AttackMoveType.Curve: await DashMove(request); break;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isAttackMoving = false;
            if (_rb) _rb.linearVelocity = Vector3.zero;

            if (_currentIsPhantom)
            {
                Physics.IgnoreLayerCollision(
                    LayerMask.NameToLayer("Player"),
                    LayerMask.NameToLayer("Enemy"), false);
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
            if (request.Duration <= 0f) return;
            if (elapsed >= request.Duration) break;
            if (request.Target &&
                Vector3.Distance(request.Target.position, transform.position) < request.StopDistance) break;

            float t = elapsed / request.Duration * _timeScale;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            _rb.MovePosition(Vector3.Lerp(startPos, targetPos, smoothT));

            elapsed += Time.fixedDeltaTime * _timeScale;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, _attackMoveCts.Token);
        }
    }

    private async UniTask StepMove(AttackMoveRequest request)
    {
        float elapsed = 0f;
        Vector3 moveDir = transform.forward; moveDir.y = 0f;
        float speed = request.Distance / request.Duration;

        while (true)
        {
            if (request.Duration <= 0f) return;
            if (elapsed >= request.Duration) break;
            if (request.Target &&
                Vector3.Distance(request.Target.position, transform.position) < request.StopDistance) break;

            _rb.linearVelocity = moveDir * speed * _timeScale;
            elapsed += Time.fixedDeltaTime * _timeScale;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, _attackMoveCts.Token);
        }
    }
}
