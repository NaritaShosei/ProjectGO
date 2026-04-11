using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class PlayerAnimationController : MonoBehaviour, IAnimationController, IModeChangeAnimationController
{
    public void Init(PlayerStateManager stateManager, IModeController modeController)
    {
        _stateManager = stateManager;
        _modeController = modeController;

        _stateManager.OnStateChanged += OnStateChanged;
        _modeController.OnModeChanged += OnModeChanged;
    }

    public event Action OnAttackComplete;
    public event Action OnComboWindowStart;
    public event Action OnComboWindowEnd;
    public event Action OnAttackExecute;
    public event Action OnModeChangeComplete;
    public event Action OnComboTransition;
    public event Action OnDodgeEnd;


    /// <summary>被弾アニメーション終了イベント（PlayerMovementやPlayerが購読）</summary>
    public event Action OnDamagedEnd;

    // ── IAnimationController ──────────────────────────────────
    public void AnimEvent_AttackExecute() => OnAttackExecute?.Invoke();
    public void AnimEvent_AttackComplete() => OnAttackComplete?.Invoke();
    public void AnimEvent_ComboWindowStart() => OnComboWindowStart?.Invoke();
    public void AnimEvent_ComboWindowEnd() => OnComboWindowEnd?.Invoke();
    public void AnimEvent_ModeChangeComplete() => OnModeChangeComplete?.Invoke();
    public void AnimEvent_ComboTransition() => OnComboTransition?.Invoke();

    /// <summary>被弾アニメーション終了をSMBから受け取る</summary>
    public void AnimEvent_DamagedEnd() => OnDamagedEnd?.Invoke();

    public void AnimEvent_DodgeEnd() => OnDodgeEnd?.Invoke();

    // ── 移動アニメーション ───────────────────────────────────

    /// <summary>
    /// 通常移動: Speed パラメータを更新してBlendTreeでアニメーション切り替え。
    /// </summary>
    public void UpdateMoveAnimation(float speed)
    {
        _animator.SetFloat(AnimParams.Speed, speed, 0.1f, Time.deltaTime);
        _animator.SetFloat(AnimParams.MoveX, 0f, 0.1f, Time.deltaTime);
        _animator.SetFloat(AnimParams.MoveY, speed, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// ロックオン中の8方向移動: MoveX / MoveY を更新。
    /// inputDir はカメラ空間の入力方向、playerForward はプレイヤー正面方向。
    /// </summary>
    public void UpdateLockedMoveAnimation(Vector2 inputDir, Vector3 playerForward, Vector3 cameraRight)
    {
        // ワールド空間の移動方向をプレイヤーローカル空間に変換
        Vector3 worldDir = (cameraRight * inputDir.x + Vector3.ProjectOnPlane(
            Camera.main ? Camera.main.transform.forward : Vector3.forward, Vector3.up).normalized * inputDir.y);
        worldDir.y = 0f;

        float magnitude = worldDir.magnitude;
        float localX = 0f, localY = 0f;

        if (magnitude > 0.01f)
        {
            Vector3 normDir = worldDir.normalized;
            Vector3 fwd = playerForward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

            localX = Vector3.Dot(normDir, right) * magnitude;
            localY = Vector3.Dot(normDir, fwd) * magnitude;
        }

        _animator.SetFloat(AnimParams.MoveX, localX, 0.1f, Time.deltaTime);
        _animator.SetFloat(AnimParams.MoveY, localY, 0.1f, Time.deltaTime);
        _animator.SetFloat(AnimParams.Speed, magnitude, 0.1f, Time.deltaTime);
    }

    // ── 攻撃アニメーション ───────────────────────────────────

    public void PlayAttack(int attackId)
    {
        _animator.SetInteger(AnimParams.AttackId, attackId);
        _animator.SetTrigger(AnimParams.Attack);
    }

    public void PlayAttackBlend(int attackId, string stateName, float transitionDuration = 0.1f)
    {
        _animator.SetInteger(AnimParams.AttackId, attackId);

        if (!string.IsNullOrEmpty(stateName))
        {
            _animator.CrossFadeInFixedTime(stateName, transitionDuration, 0);
        }
        else
        {
            _animator.SetTrigger(AnimParams.Attack);
        }
    }

    // ── 回避アニメーション ───────────────────────────────────

    /// <summary>
    /// 通常回避（ロックオンなし）: ステート名を直接指定して再生。
    /// </summary>
    public void PlayDodge()
    {
        _animator.SetFloat(AnimParams.DodgeX, 0);
        _animator.SetFloat(AnimParams.DodgeY, 1);
        _animator.SetTrigger(AnimParams.Dodge);
    }

    /// <summary>
    /// ロックオン中の8方向回避: DodgeX / DodgeY をセットしてトリガー発火。
    /// inputDir はカメラ空間の入力方向をプレイヤーローカルに変換した値。
    /// </summary>
    public void PlayLockedDodge(float localX, float localY, float transitionDuration = 0.1f)
    {
        _animator.SetFloat(AnimParams.DodgeX, localX);
        _animator.SetFloat(AnimParams.DodgeY, localY);
        _animator.SetTrigger(AnimParams.Dodge);
    }

    // ── ヒットストップ ───────────────────────────────────────

    public void SetAnimSpeed(float speed)
    {
        if (speed == 1f)
        {
            if (_isSpeedChanging)
            {
                _animator.speed = _beforeAnimSpeed;
                _isSpeedChanging = false;
            }
            return;
        }

        if (!_isSpeedChanging)
        {
            _beforeAnimSpeed = _animator.speed;
            _isSpeedChanging = true;
        }

        _animator.speed = _beforeAnimSpeed * speed;
    }

    /// ── その他 ─────────────────────────────────────────────
    /// <summary>
    /// ロックオンのON/OFFをアニメーションに伝える。
    /// </summary>
    public void SetLockedOn(bool isLockedOn)
    {
        _animator.SetBool(AnimParams.IsLockedOn, isLockedOn);
    }

    public void OnDestroy()
    {
        if (_stateManager != null)
            _stateManager.OnStateChanged -= OnStateChanged;
        if (_modeController != null)
            _modeController.OnModeChanged -= OnModeChanged;
    }

    // ── Inspector ───────────────────────────────────────────
    [SerializeField] private Animator _animator;

    // ── Private ─────────────────────────────────────────────
    private PlayerStateManager _stateManager;
    private IModeController _modeController;
    private float _beforeAnimSpeed = 1f;
    private bool _isSpeedChanging = false;

    private static class AnimParams
    {
        public const string Body = "BodyUpper";

        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int MoveX = Animator.StringToHash("MoveX");
        public static readonly int MoveY = Animator.StringToHash("MoveY");
        public static readonly int DodgeX = Animator.StringToHash("DodgeX");
        public static readonly int DodgeY = Animator.StringToHash("DodgeY");

        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int AttackId = Animator.StringToHash("AttackId");
        public static readonly int Dodge = Animator.StringToHash("Dodge");
        public static readonly int IsCharging = Animator.StringToHash("IsCharging");
        public static readonly int Damaged = Animator.StringToHash("Damaged");
        public static readonly int Dead = Animator.StringToHash("Dead");
        public static readonly int PlayerMode = Animator.StringToHash("PlayerMode");
        public static readonly int ModeChange = Animator.StringToHash("ModeChange");
        public static readonly int IsLockedOn = Animator.StringToHash("IsLockedOn");
    }

    private void Awake()
    {
        // BodyUpperレイヤーは不要になったので取得のみ
        // （攻撃のアニメーションレイヤー管理はAttackData側に委ねる）
    }

    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Charging:
                _animator.SetBool(AnimParams.IsCharging, true);
                break;
            case PlayerState.Dead:
                _animator.SetTrigger(AnimParams.Dead);
                break;
            case PlayerState.Damaged:
                _animator.SetTrigger(AnimParams.Damaged);
                break;
        }

        if (oldState == PlayerState.Charging)
        {
            _animator.SetBool(AnimParams.IsCharging, false);
        }
    }

    private void OnModeChanged(PlayerMode newMode)
    {
        // Warrior→Thunderのモードチェンジはアニメーションをスキップ
        if (newMode == PlayerMode.Warrior)
        {
            _animator.SetInteger(AnimParams.PlayerMode, (int)newMode);
            OnModeChangeComplete?.Invoke();
            return;
        }

        // Thunderへの切替: トリガーを先に発火してからPlayerModeを更新しない
        // PlayerModeの更新はModeChangeSMBのmodeChangeEndTime後に行う
        _animator.SetTrigger(AnimParams.ModeChange);
    }
}
