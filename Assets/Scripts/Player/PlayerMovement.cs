using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.AppUI.Core;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const float INPUT_THRESHOLD = 0.001f;

    public event Action OnStartDodgeInvincible;
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
        _attack.OnAttackMoveStopRequested += HandleAttackMoveStop;

        _attack.OnAttackEnded += HandleAttackEnd;
        _animationController.OnDamagedEnd += HandleDamagedEnd;

        _animationController.OnDodgeInvincibilityStart += HandleDodgeInvincibilityStart;
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

    public void AddModifier(IStatModifier modifier)
    {
        if (!_modifiers.Contains(modifier))
            _modifiers.Add(modifier);
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

    private bool _wasMoving;
    private bool _isDodging;
    private CancellationTokenSource _dodgeMoveCts;
    private CancellationTokenSource _attackMoveCts;
    private bool _isAttackMoving;
    private bool _currentIsPhantom;

    private DodgeData _currentDodgeData;

    private List<IStatModifier> _modifiers = new List<IStatModifier>();

    private float InvincibleDuration
    {
        get
        {
            float value = _currentDodgeData.InvincibleDuration;

            foreach (var modifier in _modifiers)
            {
                value = modifier.Modify(value);
            }

            return value;
        }
    }

    private void Update()
    {
        if (!_isAttackMoving)
        {
            Rotate();
            PlayMoveAnimation();
            CheckMoveStart();
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
        if (_attack != null)
        {
            _attack.OnAttackMoveRequested -= HandleAttackMove;
            _attack.OnAttackMoveStopRequested -= HandleAttackMoveStop;
            _attack.OnAttackEnded -= HandleAttackEnd;
        }

        if (_animationController != null)
        {
            _animationController.OnDamagedEnd -= HandleDamagedEnd;
            _animationController.OnDodgeInvincibilityStart -= HandleDodgeInvincibilityStart;
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
        {
            var input = _input.MoveInput;
            var snappedInput = SnapTo8Directions(input);

            _animationController.UpdateLockedMoveAnimation(
               snappedInput, transform.forward, _cameraManager.MainCamera.transform.right);
        }
        else
            _animationController.UpdateMoveAnimation(_rb.linearVelocity.magnitude);
    }

    /// <summary>
    /// 移動入力が入り、かつ移動可能な状態になった瞬間を検知する。
    /// 停止状態から移動状態へ遷移した際に MoveCrossFade を実行する。
    /// </summary>
    private void CheckMoveStart()
    {
        bool isMoving =
            _playerStateManager.CanMove() &&
            _input.MoveInput.magnitude > INPUT_THRESHOLD;

        if (!_wasMoving && isMoving)
        {
            _animationController.MoveCrossFade();
        }

        _wasMoving = isMoving;
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

        _currentDodgeData = _moveData.GetDodge(_modeController.CurrentMode);
        Vector3 dodgeDir = GetDodgeDirection();

        if (_lockOnTarget != null || isCancelDodge)
        {
            // ロックオン中の回避は入力方向に応じた8方向アニメーションで回避する。
            var snappedInput = SnapTo8Directions(_input.MoveInput);
            var cam = _cameraManager.MainCamera.transform;

            // 入力方向をワールド空間のベクトルに変換してプレイヤーローカルに変換
            var snappedWorldDir =
            Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized * snappedInput.x +
            Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized * snappedInput.y;

            // ワールド空間の回避方向をプレイヤーローカル空間に変換してアニメーションに渡す
            var localDir = transform.InverseTransformDirection(snappedWorldDir.normalized);

            // ロックオン中の回避アニメーションは8方向に分かれているため、入力方向を丸めてアニメーションを切り替える
            _animationController.PlayLockedDodge(localDir.x, localDir.z);
        }
        else
        {
            // 通常回避は前方向に向き直して前回避アニメーション
            if (dodgeDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dodgeDir);

            _animationController.PlayDodge();
        }

        DodgeMoveAsync(dodgeDir, _currentDodgeData.Speed, _currentDodgeData.Duration, _dodgeMoveCts.Token).Forget();
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
    /// 回避開始のSMBから無敵状態を開始するハンドラー。回避開始のタイミングでステートをDodgeに変更する。これにより、回避中は移動や攻撃ができなくなる。
    /// </summary>
    private void HandleDodgeInvincibilityStart()
    {
        // 回避開始のタイミングでステートをDodgeに変更する。これにより、回避中は移動や攻撃ができなくなる。
        if (!_isDodging) return;

        OnStartDodgeInvincible?.Invoke();

        _playerStateManager.AddInvincible(InvincibleType.Dodge);

        HandleDodgeInvincibilityEnd().Forget();
    }

    /// <summary>
    ///　回避無敵状態を終了する非同期メソッド。回避開始から一定時間が経過したら、無敵状態を解除する。
    /// </summary>
    private async UniTaskVoid HandleDodgeInvincibilityEnd()
    {
        float elapsed = 0f;

        try
        {
            while (elapsed < InvincibleDuration)
            {
                elapsed += Time.deltaTime * _timeScale;
                await UniTask.Yield(_dodgeMoveCts.Token, false);
            }
        }
        catch (OperationCanceledException)
        {
            // 回避移動がキャンセルされた場合も無敵状態を解除するため、ここで例外をキャッチして処理を続行する。
        }
        finally
        {
            _playerStateManager.RemoveInvincible(InvincibleType.Dodge);
        }
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
            await DashMove(request);
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
    /// ダッシュ移動の実装。
    /// </summary>
    private async UniTask DashMove(AttackMoveRequest request)
    {
        if (request.Duration <= 0f)
            return;

        AnimationCurve curve = request.MoveCurve;

        float elapsed = 0f;

        Vector3 startPos = transform.position;

        Vector3 dir = request.Direction;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f)
        {
            dir = transform.forward;
            dir.y = 0f;
        }
        dir = dir.normalized;

        Vector3 targetPos = startPos + dir * request.Distance;

        bool stoppedEarly = false;

        while (elapsed < request.Duration)
        {
            if (request.Target &&
                Vector3.Distance(request.Target.position, transform.position) < request.StopDistance)
            {
                stoppedEarly = true;
                break;
            }

            float t = elapsed / request.Duration;
            float curveValue = curve.Evaluate(t);

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, curveValue);

            _rb.MovePosition(newPos);

            elapsed += Time.fixedDeltaTime * _timeScale;

            await UniTask.Yield(
                PlayerLoopTiming.FixedUpdate,
                _attackMoveCts.Token);
        }
        if (!stoppedEarly)
            _rb.MovePosition(targetPos);
    }

    /// <summary>
    /// 攻撃アニメーション終了時のハンドラー。
    /// </summary>
    private void HandleAttackEnd()
    {   
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
        _attackMoveCts = null;
    }

    private void HandleAttackMoveStop()
    {
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
        _attackMoveCts = null;
        _isAttackMoving = false;
        if (_rb) _rb.linearVelocity = Vector3.zero;
    }

    /// <summary>
    /// 入力方向を8方向に丸める。ロックオン中の回避や攻撃移動で使用する。
    /// </summary>
    private Vector2 SnapTo8Directions(Vector2 input)
    {
        if (input.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        // 角度取得（ラジアン → 度）
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

        // 45度刻みに丸める
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;

        // ベクトルに戻す
        float rad = snappedAngle * Mathf.Deg2Rad;
        Vector2 result = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        return result.normalized;
    }
}
