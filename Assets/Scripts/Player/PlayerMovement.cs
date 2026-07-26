using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const float INPUT_THRESHOLD = 0.001f;
    private const float ATTACK_MOVE_CAST_SKIN = 0.02f;
    private const float ATTACK_MOVE_MIN_CAST_DISTANCE = 0.001f;
    private const float DAMAGE_MOVE_CAST_SKIN = 0.02f;

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

    /// <summary>
    /// リアクション強度に応じた被弾移動を開始する。
    /// </summary>
    public void PlayDamageReaction(DamageReactionType reactionType)
    {
        _damageReactionMoveCts?.Cancel();
        _damageReactionMoveCts?.Dispose();
        _damageReactionMoveCts = new CancellationTokenSource();

        if (reactionType == DamageReactionType.Small) return;

        bool isMedium = reactionType == DamageReactionType.Medium;
        PerformDamageReactionMove(
            reactionType,
            isMedium ? _mediumReactionDistance : _largeReactionDistance,
            isMedium ? _mediumReactionDuration : _largeReactionDuration,
            isMedium ? _mediumReactionMoveCurve : _largeReactionMoveCurve,
            _damageReactionMoveCts.Token).Forget();
    }

    [SerializeField] private Rigidbody _rb;

    [Header("Damage Reaction")]
    [SerializeField, Min(0f)] private float _mediumReactionDistance = 1.5f;
    [SerializeField, Min(0.01f)] private float _mediumReactionDuration = 0.35f;
    [SerializeField]
    private AnimationCurve _mediumReactionMoveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float _largeReactionDistance = 1.2f;
    [SerializeField, Min(0f)] private float _largeReactionHeight = 1.5f;
    [SerializeField, Min(0.01f)] private float _largeReactionDuration = 0.6f;
    [SerializeField]
    private AnimationCurve _largeReactionMoveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField]
    private AnimationCurve _largeReactionHeightCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -4f, 0f));

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
    private CancellationTokenSource _damageReactionMoveCts;
    private bool _isAttackMoving;
    private bool _currentIsPhantom;
    private float _attackMoveElapsed;
    private int _attackMoveVersion;
    private int _enemyLayerMask;

    private DodgeData _currentDodgeData;

    private List<IStatModifier> _modifiers = new List<IStatModifier>();

    private void Awake()
    {
        _enemyLayerMask = LayerMask.GetMask("Enemy");
    }

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
        _damageReactionMoveCts?.Cancel();
        _damageReactionMoveCts?.Dispose();
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
        Vector3 cameraRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
        Vector3 cameraForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        Vector3 moveDir = (cameraRight * vec.x + cameraForward * vec.y).normalized;
        moveDir.y = 0f;

        _rb.linearVelocity = moveDir * _modeController.ModeData.MoveSpeed * inputMag * _timeScale;
    }

    /// <summary> 回転処理。ロックオン中は常に敵の方向を向く。ロックオンなしは移動入力の方向を向く。 </summary>
    private void Rotate()
    {
        if (!_playerStateManager.CanMove()) return;

        if (_lockOnTarget != null && _modeController.CurrentMode != PlayerMode.Thunder)
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

        if (_lockOnTarget != null && _modeController.CurrentMode != PlayerMode.Thunder)
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
    /// 被弾アニメーション終了時にDamaged状態を解除する。
    /// </summary>
    private void HandleDamagedEnd()
    {
        if (_playerStateManager.IsDamaged())
            _playerStateManager.ChangeState(PlayerState.Idle);
    }

    private async UniTaskVoid PerformDamageReactionMove(
        DamageReactionType reactionType,
        float distance,
        float duration,
        AnimationCurve moveCurve,
        CancellationToken cancellationToken)
    {
        Vector3 startPosition = _rb.position;
        Vector3 backward = -transform.forward;
        backward.y = 0f;
        backward.Normalize();
        bool isLarge = reactionType == DamageReactionType.Large;
        RigidbodyConstraints originalConstraints = _rb.constraints;

        if (isLarge)
        {
            // 通常移動ではY位置を固定しているため、打ち上げ中だけ解除する。
            // 上下位置そのものは物理速度ではなくAnimationCurveで決定する。
            _rb.constraints = originalConstraints & ~RigidbodyConstraints.FreezePositionY;
        }

        float elapsed = 0f;
        try
        {
            while (elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime * _timeScale;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float horizontalDistance = moveCurve.Evaluate(normalizedTime) * distance;
                Vector3 candidate = startPosition + backward * horizontalDistance;

                candidate = ResolveDamageReactionWall(_rb.position, candidate);
                candidate.y = isLarge
                    ? startPosition.y
                      + _largeReactionHeightCurve.Evaluate(normalizedTime) * _largeReactionHeight
                    : startPosition.y;

                _rb.MovePosition(candidate);
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 新しいリアクションまたは破棄による中断。
        }
        finally
        {
            if (isLarge && _rb)
            {
                // 中断された場合も地面の高さへ戻し、元の移動制約を復元する。
                Vector3 landedPosition = _rb.position;
                landedPosition.y = startPosition.y;
                _rb.position = landedPosition;
                _rb.constraints = originalConstraints;
            }
        }
    }

    private Vector3 ResolveDamageReactionWall(Vector3 currentPosition, Vector3 candidatePosition)
    {
        Vector3 horizontalDelta = candidatePosition - currentPosition;
        horizontalDelta.y = 0f;
        float distance = horizontalDelta.magnitude;
        if (distance <= ATTACK_MOVE_MIN_CAST_DISTANCE) return candidatePosition;

        RaycastHit[] hits = _rb.SweepTestAll(
            horizontalDelta / distance,
            distance + DAMAGE_MOVE_CAST_SKIN,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider || hit.normal.y > 0.5f) continue;
            nearestDistance = Mathf.Min(nearestDistance, hit.distance);
        }

        if (nearestDistance == float.MaxValue) return candidatePosition;

        float safeDistance = Mathf.Max(0f, nearestDistance - DAMAGE_MOVE_CAST_SKIN);
        Vector3 resolved = currentPosition
            + horizontalDelta.normalized * Mathf.Min(safeDistance, distance);
        resolved.y = candidatePosition.y;
        return resolved;
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
        if (!request.Resume)
            _attackMoveElapsed = 0f;

        int moveVersion = ++_attackMoveVersion;

        if (_currentIsPhantom)
        {
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
            _currentIsPhantom = false;
        }

        PerformAttackMove(request, moveVersion, _attackMoveCts.Token).Forget();
    }

    /// <summary>
    /// 攻撃移動の実行。リクエストの内容に応じて、ダッシュ移動・ステップ移動・カーブ移動などを行う。
    /// </summary>
    private async UniTaskVoid PerformAttackMove(
        AttackMoveRequest request,
        int moveVersion,
        CancellationToken cancellationToken)
    {
        _isAttackMoving = true;

        if (request.IsPhantom)
        {
            _currentIsPhantom = true;
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
        }

        try
        {
            await DashMove(request, cancellationToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (moveVersion == _attackMoveVersion)
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
    }

    /// <summary>
    /// ダッシュ移動の実装。
    /// </summary>
    private async UniTask DashMove(AttackMoveRequest request, CancellationToken cancellationToken)
    {
        if (request.Duration <= 0f)
            return;

        AnimationCurve curve = request.MoveCurve;

        float elapsed = Mathf.Clamp(_attackMoveElapsed, 0f, request.Duration);

        Vector3 dir = request.Direction;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f)
        {
            dir = transform.forward;
            dir.y = 0f;
        }
        dir = dir.normalized;

        float previousCurveValue = curve.Evaluate(elapsed / request.Duration);

        while (elapsed < request.Duration)
        {
            float nextElapsed = Mathf.Min(
                elapsed + Time.fixedDeltaTime * _timeScale,
                request.Duration);
            float curveValue = curve.Evaluate(nextElapsed / request.Duration);
            float curveDelta = curveValue - previousCurveValue;
            Vector3 newPos = transform.position + dir * request.Distance * curveDelta;

            if (!request.IsPhantom && TryClampAttackMoveToEnemyCollider(transform.position, newPos, out var stoppedPos))
            {
                MoveAttackPosition(stoppedPos, request.Target);
            }
            else if (MoveAttackPosition(newPos, request.Target))
            {
                break;
            }

            // 停止距離内でも時間とカーブは進める。
            // 停止中の移動量は後から取り戻さず、アニメーションとの同期を維持する。
            elapsed = nextElapsed;
            _attackMoveElapsed = elapsed;
            previousCurveValue = curveValue;

            await UniTask.Yield(
                PlayerLoopTiming.FixedUpdate,
                cancellationToken);
        }
    }

    private bool MoveAttackPosition(Vector3 candidatePos, Transform target)
    {
        // 攻撃移動は1フレームの移動量が大きいため、MovePosition前に進行方向へスイープして貫通を防ぐ。
        if (TryResolveAttackMoveCollision(transform.position, candidatePos, target, out var resolvedPos))
        {
            _rb.MovePosition(resolvedPos);
            return true;
        }

        _rb.MovePosition(candidatePos);
        return false;
    }

    private bool TryResolveAttackMoveCollision(
        Vector3 currentPos,
        Vector3 candidatePos,
        Transform target,
        out Vector3 resolvedPos)
    {
        resolvedPos = candidatePos;

        Vector3 delta = candidatePos - currentPos;
        float distance = delta.magnitude;
        if (distance <= ATTACK_MOVE_MIN_CAST_DISTANCE) return false;

        Vector3 direction = delta / distance;
        // Rigidbody自身の形状を移動先まで投げ、途中の壁や障害物を検出する。
        var hits = _rb.SweepTestAll(
            direction,
            distance + ATTACK_MOVE_CAST_SKIN,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0) return false;

        float nearestDistance = float.MaxValue;
        foreach (var hit in hits)
        {
            if (!hit.collider) continue;
            if (ShouldIgnoreAttackMoveHit(hit.collider, target)) continue;
            // 床や段差の上面で攻撃移動が止まりすぎないよう、上向きの接触面は無視する。
            if (hit.normal.y > 0.5f) continue;

            nearestDistance = Mathf.Min(nearestDistance, hit.distance);
        }

        if (nearestDistance == float.MaxValue) return false;

        // めり込みを避けるため、衝突点より少し手前を最終位置にする。
        float safeDistance = Mathf.Max(0f, nearestDistance - ATTACK_MOVE_CAST_SKIN);
        resolvedPos = currentPos + direction * Mathf.Min(safeDistance, distance);
        return true;
    }

    private bool ShouldIgnoreAttackMoveHit(Collider hitCollider, Transform target)
    {
        if (hitCollider.isTrigger) return true;
        if (!target) return false;

        // ホーミング対象の敵コライダーでは止めず、壁などの環境コライダーだけで止める。
        Transform hitTransform = hitCollider.transform;
        return hitTransform == target ||
               hitTransform.IsChildOf(target) ||
               target.IsChildOf(hitTransform);
    }

    private bool TryClampAttackMoveToEnemyCollider(
        Vector3 currentPos,
        Vector3 candidatePos,
        out Vector3 stoppedPos)
    {
        stoppedPos = candidatePos;

        Vector3 delta = candidatePos - currentPos;
        float distance = delta.magnitude;
        if (distance <= ATTACK_MOVE_MIN_CAST_DISTANCE)
            return false;

        Vector3 direction = delta / distance;
        if (!Physics.Raycast(
                currentPos,
                direction,
                out RaycastHit hit,
                distance + ATTACK_MOVE_CAST_SKIN,
                _enemyLayerMask,
                QueryTriggerInteraction.Collide))
        {
            return false;
        }

        // Enemyレイヤーのコライダー表面より少し手前で止め、敵を貫通しないようにする。
        float safeDistance = Mathf.Max(0f, hit.distance - ATTACK_MOVE_CAST_SKIN);
        stoppedPos = currentPos + direction * Mathf.Min(safeDistance, distance);
        stoppedPos.y = candidatePos.y;
        return true;
    }

    /// <summary>
    /// 攻撃アニメーション終了時のハンドラー。
    /// </summary>
    private void HandleAttackEnd()
    {
        _attackMoveVersion++;
        _attackMoveElapsed = 0f;
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
        _attackMoveCts = null;
        EndAttackMoveState();
    }

    private void HandleAttackMoveStop()
    {
        _attackMoveVersion++;
        _attackMoveCts?.Cancel();
        _attackMoveCts?.Dispose();
        _attackMoveCts = null;
        EndAttackMoveState();
    }

    private void EndAttackMoveState()
    {
        _isAttackMoving = false;
        if (_rb) _rb.linearVelocity = Vector3.zero;
        if (_currentIsPhantom)
        {
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
            _currentIsPhantom = false;
        }
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
