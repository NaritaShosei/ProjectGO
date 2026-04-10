using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    const float INPUT_THRESHOLD = 0.001f;

    public event Action OnEndDodge;

    /// <summary>
    /// PlayerStateManager, InputHandler, MoveData, IModeController, PlayerAnimationController, PlayerAttack を受け取って初期化する。
    /// </summary>
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
        _animationController.OnDamagedEnd += HandleDamagedEnd;
        _animationController.OnDodgeEnd += HandleDodgeEnd;

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
            _cameraManager = cameraManager;
        else
            Debug.LogError($"[{this}]: CameraManagerが見つかりませんでした。");
    }

    /// <summary> 時間のスケールを設定する。攻撃移動や回避移動の速度に影響する </summary>
    public void SetTimeScale(float scale) => _timeScale = scale;
    /// <summary> ロックオン対象を設定する。nullを渡すとロックオン解除。 </summary>
    public void SetLockOnTarget(Transform target) => _lockOnTarget = target;

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
    private CancellationTokenSource _dodgeMoveCts;
    private CancellationTokenSource _attackMoveCts;
    private bool _isAttackMoving;
    private bool _currentIsPhantom;

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
        if (_animationController != null)
        {
            _animationController.OnDamagedEnd -= HandleDamagedEnd;
            _animationController.OnDodgeEnd -= HandleDodgeEnd;
        }
        _dodgeMoveCts?.Cancel();
        _dodgeMoveCts?.Dispose();
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
    }

    // ── 移動 ─────────────────────────────────────────────────
    /// <summary> 移動入力に基づいてプレイヤーを移動させる。</summary>
    private void Move()
    {
        if (_isDodging) return; // 回避中は移動入力を無視（回避移動はDodgeMoveAsyncで管理）
        if (!_playerStateManager.CanMove()) { _rb.linearVelocity = Vector3.zero; return; }

        var vec = _input.MoveInput;
        float inputMag = vec.magnitude;
        if (inputMag < INPUT_THRESHOLD) { _rb.linearVelocity = Vector3.zero; return; }

        var camera = _cameraManager.MainCamera;
        Vector3 moveDir;
        if (_lockOnTarget != null)
        {
            Vector3 cameraRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            moveDir = (cameraRight * vec.x
                            + Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized * vec.y).normalized;
        }
        else
        {
            Vector3 cameraRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            Vector3 cameraForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            moveDir = (cameraRight * vec.x + cameraForward * vec.y).normalized;
        }
        moveDir.y = 0f;

        _rb.linearVelocity = moveDir * _modeController.ModeData.MoveSpeed * inputMag * _timeScale;
    }

    /// <summary> 回転処理。ロックオン中は常に敵の方向を向く。ロックオンなしは移動入力の方向を向く。 </summary>
    private void Rotate()
    {
        if (!_playerStateManager.CanMove()) return;

        if (_lockOnTarget != null)
        {
            Vector3 toTarget = _lockOnTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(toTarget),
                    _moveData.RotateSpeed * Time.deltaTime * _timeScale);
            return;
        }

        var vec = _input.MoveInput;
        if (vec.magnitude < INPUT_THRESHOLD) return;
        var cam = _cameraManager.MainCamera;
        var lookDir = cam.transform.right * vec.x + cam.transform.forward * vec.y;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude <= 0f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(lookDir),
            _moveData.RotateSpeed * Time.deltaTime * _timeScale);
    }

    /// <summary> 移動アニメーションの更新。ロックオン中は入力方向に応じた8方向アニメーション、ロックオンなしはSpeedパラメータで通常移動アニメーション。 </summary>
    private void PlayMoveAnimation()
    {
        if (_playerStateManager.IsDodging()) return;
        if (_lockOnTarget != null)
            _animationController.UpdateLockedMoveAnimation(
                _input.MoveInput, transform.forward, _cameraManager.MainCamera.transform.right);
        else
            _animationController.UpdateMoveAnimation(_rb.linearVelocity.magnitude);
    }

    // ── 回避 ─────────────────────────────────────────────────

    /// <summary>
    /// 回避処理。回避可能な状態であれば、入力方向に応じて回避アニメーションを再生し、一定時間無敵で移動する。
    /// </summary>
    private void Dodge()
    {
        if (!_playerStateManager.CanDodge()) return;

        // 攻撃キャンセル回避かどうかを記録
        bool isCancelDodge = _playerStateManager.CurrentState == PlayerState.Attacking;

        if (isCancelDodge)
        {
            _attack.InterruptByDodge();
            _attackMoveCts?.Cancel();
            _attackMoveCts?.Dispose();
            _attackMoveCts = null;
            _isAttackMoving = false;
        }

        _dodgeMoveCts?.Cancel();
        _dodgeMoveCts?.Dispose();
        _dodgeMoveCts = new CancellationTokenSource();

        _isDodging = true;
        _playerStateManager.ChangeState(PlayerState.Dodge);

        DodgeData dodgeData = _moveData.GetDodge(_modeController.CurrentMode);
        Vector3 dodgeDir = GetDodgeDirection();

        if (_lockOnTarget != null || isCancelDodge)
        {
            // ロックオン中は常に8方向
            // 攻撃キャンセル回避は8方向アニメーション
            // プレイヤーの現在の向き（敵の方向）を基準にローカル方向を計算する
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            _animationController.PlayLockedDodge(
                Vector3.Dot(dodgeDir, right),
                Vector3.Dot(dodgeDir, fwd));
        }
        else
        {
            // 通常回避は前方向に向き直して前回避アニメーション
            if (dodgeDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dodgeDir);

            _animationController.PlayDodge();
        }

        DodgeMoveAsync(dodgeDir, dodgeData.Speed, dodgeData.Duration, _dodgeMoveCts.Token).Forget();
    }

    /// <summary>
    /// 回避移動を管理する非同期メソッド。一定時間、指定された方向に移動し続ける。
    /// </summary>
    private async UniTaskVoid DodgeMoveAsync(Vector3 dir, float speed, float duration, CancellationToken ct)
    {
        float elapsed = 0f;
        try
        {
            while (elapsed < duration)
            {
                _rb.linearVelocity = dir * speed * _timeScale;
                elapsed += Time.deltaTime * _timeScale;
                await UniTask.Yield(ct);
            }
        }
        catch (OperationCanceledException) { }

        if (_rb) _rb.linearVelocity = Vector3.zero;
        // ステート復帰は HandleDodgeEnd（DodgeSMBのOnStateExit通知）を待つ
    }

    /// <summary>
    /// DodgeSMB の OnStateExit → PlayerAnimationController.AnimEvent_DodgeEnd → ここ。
    /// アニメーションが実際に終わったタイミングでステートを復帰させる。
    /// </summary>
    private void HandleDodgeEnd()
    {
        if (!_isDodging) return;
        _isDodging = false;
        _playerStateManager.ChangeState(PlayerState.Idle);
        OnEndDodge?.Invoke();
    }

    /// <summary>
    /// 回避入力の方向をワールド空間で計算する。入力がない場合は前方に回避する。
    /// </summary>
    private Vector3 GetDodgeDirection()
    {
        var input = _input.MoveInput;
        if (input.magnitude > INPUT_THRESHOLD)
        {
            var dir = _cameraManager.MainCamera.transform.right * input.x
                    + _cameraManager.MainCamera.transform.forward * input.y;
            dir.y = 0f;
            return dir.normalized;
        }
        return transform.forward;
    }

    // ── 被弾 ─────────────────────────────────────────────────

    /// <summary>
    /// 被弾アニメーション終了イベントのハンドラー。PlayerStateManagerがDamaged状態ならIdleに遷移させる。
    /// </summary>
    private void HandleDamagedEnd()
    {
        if (_playerStateManager.IsDamaged())
            _playerStateManager.ChangeState(PlayerState.Idle);
    }

    // ── 攻撃移動 ─────────────────────────────────────────────

    /// <summary>
    /// PlayerAttackから攻撃移動のリクエストを受け取るハンドラー。現在の攻撃移動をキャンセルして新しい攻撃移動を開始する。
    /// </summary>
    private void HandleAttackMove(AttackMoveRequest request)
    {
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
        _attackMoveCts = new CancellationTokenSource();

        if (_currentIsPhantom)
        {
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
            _currentIsPhantom = false;
        }

        PerformAttackMove(request).Forget();
    }

    /// <summary>
    /// 攻撃移動の実行。リクエストの内容に応じて、ダッシュ移動・ステップ移動・カーブ移動などを行う。
    /// </summary>
    private async UniTaskVoid PerformAttackMove(AttackMoveRequest request)
    {
        _isAttackMoving = true;

        if (request.IsPhantom)
        {
            _currentIsPhantom = true;
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
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
                Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
                _currentIsPhantom = false;
            }
        }
    }

    /// <summary>
    /// ダッシュ移動。プレイヤーの正面方向に向かって一定距離を滑らかに移動する。
    /// </summary>
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
            if (request.Target && Vector3.Distance(request.Target.position, transform.position) < request.StopDistance) break;

            _rb.MovePosition(Vector3.Lerp(startPos, targetPos,
                 Mathf.SmoothStep(0, 1, elapsed / request.Duration)));
            elapsed += Time.fixedDeltaTime * _timeScale;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, _attackMoveCts.Token);
        }
    }

    /// <summary>
    /// ステップ移動。プレイヤーの正面方向に向かって一定速度で移動する。距離や時間ではなく、指定された時間が経過するか、ターゲットに近づくまで移動し続ける。
    /// </summary>
    private async UniTask StepMove(AttackMoveRequest request)
    {
        float elapsed = 0f;
        Vector3 moveDir = transform.forward; moveDir.y = 0f;
        float speed = request.Distance / request.Duration;

        while (true)
        {
            if (request.Duration <= 0f) return;
            if (elapsed >= request.Duration) break;
            if (request.Target && Vector3.Distance(request.Target.position, transform.position) < request.StopDistance) break;

            _rb.linearVelocity = moveDir * speed * _timeScale;
            elapsed += Time.fixedDeltaTime * _timeScale;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, _attackMoveCts.Token);
        }
    }
}
