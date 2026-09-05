using System;
using UnityEngine;
using static SoundCueNames;

/// <summary>
/// 近接攻撃Behaviour
/// AttackerSlotでスロットを確保できた場合のみ攻撃する
/// AttackPatternのDuration・BaseDamage・Cooldown・MaxHitCount・HitIntervalを参照する
/// WindUpはAnimationEventのOnAttackHit発火タイミングでアニメーション側が制御する
/// </summary>
public class MeleeAttackBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Attack; }

    /// <summary>
    /// AttackerSlot・Animator・DistanceProfileはMeleeAttackBehaviour固有の依存のためコンストラクタで受け取る
    /// </summary>
    public MeleeAttackBehaviour(EnemyServices services, Animator animator, DistanceProfile profile = null, float _enemyCooldown = 0f)
    {
        _enemyServices = services;
        _animator = animator;
        _profile = profile;
        // profileがnullの場合は背後判定を無効化する（dot < -1 は常にfalse）
        _backDotThreshold = profile != null
            ? Mathf.Cos(profile.BackAttackAngle * Mathf.Deg2Rad)
            : -1f;
        _cooldownOverride = _enemyCooldown;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _self = ctx.Owner.Self;
        _enemy = ctx.Owner;
        _enemyId = ctx.Owner.Id;
        _player = ctx.Player;
        _context = ctx.RuntimeContext;
        _enemyAnimator = ctx.EnemyAnimator;
        _state = ctx.StateContext;

        // AttackHit / AttackEndイベントを購読してアニメーションと同期する
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnAttackHit += HandleAttackHit;
            _enemyAnimator.OnAttackEnd += HandleAttackEnd;
        }
    }

    public bool CanEnter()
    {
        if (_player == null) return false;
        if (_enemyServices.AttackerSlot == null) return false;
        if (_isAttacking) return false;

        // スポーン時に確保済みのスロットを持っているかチェック
        if (!_enemyServices.AttackerSlot.IsAcquired(_enemyId)) return false;

        // パターン未選択なら攻撃不可
        if (_context.SelectedPattern == null) return false;

        // クールダウン判定（0以下で攻撃可能）
        if (_context.AttackCooldownRemaining > 0f) return false;

        // 射程チェック（XZ平面の二乗距離で比較）
        float sqrDist = CalcXZSqrDist();

        float triggerRange = _context.SelectedPattern.AttackRange * _context.SelectedPattern.AttackTriggerRatio;
        if (sqrDist > triggerRange * triggerRange) return false;

        if (_enemyServices.PlayerInformationService.IsBehaindPlayer(_self))
        {
            // Playerが接敵していなければ確率キャンセルを行わない
            if (!_enemyServices.PlayerInformationService.IsPlayerEncounteringEnemy())
            {
                return true;
            }

            // 背後攻撃抑制（プレイヤー背後にいる場合は確率的にキャンセル）
            if (_profile != null)
            {
                if (UnityEngine.Random.value < _profile.BackAttackSuppressChance) return false;
            }
        }

        return true;
    }

    public bool CanContinue()
    {
        // 攻撃開始後はアニメーション終了まで継続する
        return _isAttacking;
    }

    public void OnEnter()
    {
        _isAttacking = true;
        _timer = 0f;
        _hitCount = 0;
        _nextHitTime = float.MaxValue;
        _attackEndFired = false;
        _moveFinished = false;
        _moveCurvePrevEval = 0f;
        _state.ChangeState(EnemyState.Attack);
        _enemyAnimator?.SetAttacking(true);
    }

    public void Tick(float deltaTime)
    {
        if (!_isAttacking) return;

        _timer += deltaTime;

        var pattern = _context.SelectedPattern;

        // 前進 + はじめのみホーミング
        if (pattern != null && pattern.EnableMovement && !_moveFinished)
        {
            TickAttackMovement(pattern, deltaTime);
        }

        // 多段ヒット：deltaTimeが大きい場合も期限超過分をすべて消化する
        int maxHitCount = _context.SelectedPattern?.MaxHitCount ?? 1;
        while (_hitCount > 0 && _hitCount < maxHitCount && _timer >= _nextHitTime)
        {
            PerformHit();
        }

        // AnimationEventで終了を検知できなかった場合のフォールバック
        // Duration / clip長を超えた場合は強制終了する
        if (!_attackEndFired)
        {
            float duration = (_context.SelectedPattern != null && _context.SelectedPattern.Duration > 0f)
                ? _context.SelectedPattern.Duration
                : GetAnimationLength();

            if (_timer >= duration)
            {
                Exit(notifyAttackFinished: true);
            }
        }
    }

    public void OnExit()
    {
        if (!_isAttacking) return;
        Exit(notifyAttackFinished: false);
    }

    /// <summary>
    /// イベント購読を解除する
    /// </summary>
    public void Dispose()
    {
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnAttackHit -= HandleAttackHit;
            _enemyAnimator.OnAttackEnd -= HandleAttackEnd;
        }
    }

    /// <summary>
    /// AttackerSlotを解放する
    /// </summary>
    public void ReleaseSlot()
    {
        if (_enemyServices.AttackerSlot == null) return;
        _enemyServices.AttackerSlot.Release(_enemyId, 1);
    }

    private Transform _self;
    private IEnemy _enemy;
    private Transform _player;
    private EnemyRuntimeContext _context;
    private EnemyStateContext _state;
    private IEnemyAnimator _enemyAnimator;
    private Animator _animator;

    private readonly EnemyServices _enemyServices;
    private readonly DistanceProfile _profile;
    private readonly float _backDotThreshold;

    private int _enemyId;
    private float _timer;
    private float _nextHitTime;
    private bool _isAttacking;
    private int _hitCount;
    private bool _attackEndFired;
    private readonly float _cooldownOverride;//攻撃のCT 後々OverrideじゃなくてEnemyDataから取れるといいかも？

    // 前進移動の進捗管理
    private bool _moveFinished;
    private float _moveCurvePrevEval;

    // AnimationEventが来ない場合の攻撃強制終了タイムアウト（秒）
    private const float _attackFallbackTimeout = 5f;

    // Attackアニメーターステートの名前
    private const string _attackStateName = "Attack";

    public event Action OnAttackFinished;


    /// <summary>
    /// 実際の攻撃判定とダメージ適用を行う
    /// </summary>
    private void PerformHit()
    {
        var pattern = _context.SelectedPattern;
        if (pattern == null) return;

#if UNITY_EDITOR
        Vector3 debugCenter = _self.position + _self.forward * pattern.AttackRange;
        Debug.Log($"[Attack:{pattern.PatternName}] hit={_hitCount + 1}, center={debugCenter}, radius={pattern.AttackRadius}");
        Collider[] debugHits = Physics.OverlapSphere(debugCenter, pattern.AttackRadius);
        foreach (var h in debugHits)
        {
            Debug.Log($"[Attack:{pattern.PatternName}] target={h.gameObject.name}, hasIPlayer={h.TryGetComponent<IPlayer>(out _)}");
        }
#endif

        Collider[] hits = Physics.OverlapSphere(
            _self.position + _self.forward * pattern.AttackRange,
            pattern.AttackRadius
        );

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IPlayer player))
            {
                player.TakeDamage(pattern.BaseDamage);
            }
        }

        _hitCount++;
        _nextHitTime += pattern.HitInterval;
    }

    private void TickAttackMovement(EnemyAttackPattern pattern, float deltaTime)
    {
        if(pattern.EnableHoming && _timer <= pattern.HomingDuration && _player != null)
        {
            Vector3 toPlayer = _player.position - _self.position;
            toPlayer.y = 0f;

            if(toPlayer.sqrMagnitude > 0.1f && toPlayer.sqrMagnitude <= pattern.HomingRadius * pattern.HomingRadius)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                float angle = Quaternion.Angle(_self.rotation, targetRot);

                if(angle <= pattern.HomingAngle)
                {
                    _self.rotation = Quaternion.RotateTowards(_self.rotation, targetRot, pattern.HomingStrength * deltaTime);
                }
            }
        }

        // --- MoveCurveに従って前進量を計算 ---
        float moveT = Mathf.Clamp01(_timer / pattern.MoveDuration);
        float curEval = pattern.MoveCurve.Evaluate(moveT);
        float deltaDist = (curEval - _moveCurvePrevEval) * pattern.MoveDistance;
        _moveCurvePrevEval = curEval;

        // KeepDistanceより内側には詰めない
        if (deltaDist > 0f && _player != null)
        {
            float distToPlayer = Vector3.Distance(_self.position, _player.position);
            float maxAllowedDist = Mathf.Max(0f, distToPlayer - pattern.KeepDistance);
            deltaDist = Mathf.Min(deltaDist, maxAllowedDist);
        }

        if (deltaDist != 0f)
        {
            Vector3 oldPos = _self.position;
            Vector3 displacement = _self.forward * deltaDist;

            if (_enemy is Enemy movableEnemy)
                movableEnemy.Move(displacement);
            else
                _self.position += displacement;

            if (_enemyServices.SpatialHashGrid != null)
                _enemyServices.SpatialHashGrid.UpdatePosition(_enemy, oldPos, _self.position);
        }

        if (moveT >= 1f)
            _moveFinished = true;
    }

    private void Exit(bool notifyAttackFinished)
    {
        if (!_isAttacking) return;
        _isAttacking = false;

        // 攻撃後クールダウンをセット
        float cooldown = _cooldownOverride > 0f ? _cooldownOverride : (_context.SelectedPattern?.Cooldown ?? 1.5f);
        _context.AttackCooldownRemaining = cooldown;
       
        // パターンをクリアする。MobEnemy.UpdateEnemy()が次フレームで再選択する
        _context.SelectedPattern = null;

        _enemyAnimator?.SetAttacking(false);
        _state.ChangeState(EnemyState.Idle);

        ReleaseSlot();

        if (notifyAttackFinished)
        {
            OnAttackFinished?.Invoke();
        }
    }

    /// <summary>
    /// AttackステートのAnimationClip長を取得する
    /// AttackPattern.Durationが未設定の場合のフォールバック
    /// </summary>
    private float GetAnimationLength()
    {
        // Animatorがnullのときはフォールバックタイムアウトを返して攻撃が即終了しないようにする
        if (_animator == null) return _attackFallbackTimeout;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(_attackStateName))
        {
            return stateInfo.length;
        }

        // Attackステートに遷移前、またはClip長取得不能の場合はフォールバックタイムアウトを返す
        return _attackFallbackTimeout;
    }

    /// <summary>
    /// 攻撃ヒットタイミングのAnimationEventから中継されるハンドラ
    /// WindUpはアニメーション側でOnAttackHitの発火タイミングとして制御される
    /// </summary>
    private void HandleAttackHit()
    {
        if (!_isAttacking) return;

        // 初回ヒットのみAnimationEventで処理し、以降はTick内でHitIntervalに従って処理する
        if (_hitCount > 0) return;

        PerformHit();
    }

    /// <summary>
    /// 攻撃終了タイミングのAnimationEventから中継されるハンドラ
    /// </summary>
    private void HandleAttackEnd()
    {
        if (!_isAttacking) return;
        _attackEndFired = true;
        Exit(notifyAttackFinished: true);
    }

    /// <summary>
    /// プレイヤーとのXZ平面距離の二乗を返す。
    /// 高低差の影響を受けず、距離比較を高速に行うため平方根は計算しない。
    /// </summary>
    private float CalcXZSqrDist()
    {
        float dx = _self.position.x - _player.position.x;
        float dz = _self.position.z - _player.position.z;
        return dx * dx + dz * dz;
    }
}
