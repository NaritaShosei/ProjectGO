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
    public event Action OnModeChangeReady;
    public event Action<int, int> OnAttackExecute;
    public event Action OnModeChangeComplete;
    public event Action OnComboTransition;
    public event Action OnDodgeInvincibilityStart;
    public event Action OnDodgeEnd;
    public event Action OnChargeReady;


    /// <summary>被弾アニメーション終了イベント（PlayerMovementやPlayerが購読）</summary>
    public event Action OnDamagedEnd;

    // ── IAnimationController ──────────────────────────────────
    public void AnimEvent_AttackExecute() => AnimEvent_AttackExecute(0, 1);
    public void AnimEvent_AttackExecute(int hitIndex) => AnimEvent_AttackExecute(hitIndex, hitIndex + 1);
    public void AnimEvent_AttackExecute(int hitIndex, int hitCount) => OnAttackExecute?.Invoke(hitIndex, hitCount);
    public void AnimEvent_AttackComplete() => OnAttackComplete?.Invoke();
    public void AnimEvent_ComboWindowStart() => OnComboWindowStart?.Invoke();
    public void AnimEvent_ComboWindowEnd() => OnComboWindowEnd?.Invoke();
    public void AnimEvent_ModeChangeReady() => OnModeChangeReady?.Invoke();
    public void AnimEvent_ModeChangeComplete() => OnModeChangeComplete?.Invoke();
    public void AnimEvent_ComboTransition() => OnComboTransition?.Invoke();

    /// <summary>被弾アニメーション終了をSMBから受け取る</summary>
    public void AnimEvent_DamagedEnd() => OnDamagedEnd?.Invoke();

    public void SetDamageReaction(DamageReactionType reactionType)
    {
        _animator.SetInteger(AnimParams.DamageReaction, (int)reactionType);
    }

    public void AnimEvent_DodgeInvincibilityStart() => OnDodgeInvincibilityStart?.Invoke();
    public void AnimEvent_DodgeEnd() => OnDodgeEnd?.Invoke();

    public void AnimEvent_ChargeReady() => OnChargeReady?.Invoke();

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

    /// <summary>
    /// 現在のモードに応じた移動ステートへクロスフェードする。
    /// </summary>
    public void MoveCrossFade(float transitionDuration = 0.1f)
    {
        string stateName;

        if (_isLockedOn && _modeController.CurrentMode != PlayerMode.Thunder)
        {
            stateName = _modeController.CurrentMode switch
            {
                PlayerMode.Warrior => AnimParams.WarriorLockedMove,
                _ => null
            };
        }
        else
        {
            stateName = _modeController.CurrentMode switch
            {
                PlayerMode.Warrior => AnimParams.WarriorFreeMove,
                PlayerMode.Thunder => AnimParams.ThunderFreeMove,
                _ => null
            };
        }

        if (!string.IsNullOrEmpty(stateName))
        {
            _animator.CrossFadeInFixedTime(stateName, transitionDuration);
        }
    }

    // ── 攻撃アニメーション ───────────────────────────────────

    public void PlayAttack(int attackId)
    {
        _animator.SetInteger(AnimParams.AttackId, attackId);
        _animator.SetTrigger(AnimParams.Attack);
    }

    public void PlayAttackBlend(int attackId, string stateName, float transitionDuration = 0.1f)
    {
        if (!string.IsNullOrEmpty(stateName))
        {
            _animator.CrossFadeInFixedTime(stateName, transitionDuration, 0);
        }
        else
        {
            _animator.SetInteger(AnimParams.AttackId, attackId);
            _animator.SetTrigger(AnimParams.Attack);
        }
    }

    /// <summary>
    /// チャージアニメーションを再生する。
    /// 各チャージ段階のAttackDataが持つChargeAnimationStateNameで直接遷移。
    /// </summary>
    public void PlayChargeAnimation(string stateName, float transitionDuration = 0.1f)
    {
        if (string.IsNullOrEmpty(stateName)) return;
        _animator.CrossFadeInFixedTime(stateName, transitionDuration, 0);
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
        _isLockedOn = isLockedOn;
        ApplyLockedOnAnimationParameter(_modeController.CurrentMode);
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
    private bool _isLockedOn = false;

    private static class AnimParams
    {
        // Layer
        public const int BaseLayer = 0;

        // State
        public const string WarriorFreeMove =
            "Base Layer.Warrior.Warrior_FreeMove";

        public const string WarriorLockedMove =
            "Base Layer.Warrior.Warrior_LockedMove";

        public const string ThunderFreeMove =
            "Base Layer.Thunder.Thunder_FreeMove";

        public const string ModeChangeToThunder =
            "ModeChangeToThunder";

        // Float
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int MoveX = Animator.StringToHash("MoveX");
        public static readonly int MoveY = Animator.StringToHash("MoveY");
        public static readonly int DodgeX = Animator.StringToHash("DodgeX");
        public static readonly int DodgeY = Animator.StringToHash("DodgeY");

        // Int
        public static readonly int AttackId = Animator.StringToHash("AttackId");
        public static readonly int PlayerMode = Animator.StringToHash("PlayerMode");
        public static readonly int DamageReaction = Animator.StringToHash("DamageReaction");

        // Bool
        public static readonly int IsCharging = Animator.StringToHash("IsCharging");
        public static readonly int IsLockedOn = Animator.StringToHash("IsLockedOn");

        // Trigger
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int Dodge = Animator.StringToHash("Dodge");
        public static readonly int Damaged = Animator.StringToHash("Damaged");
        public static readonly int Dead = Animator.StringToHash("Dead");
        public static readonly int ModeChange = Animator.StringToHash("ModeChange");
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
        ApplyLockedOnAnimationParameter(newMode);

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

        _animator.CrossFadeInFixedTime(AnimParams.ModeChangeToThunder, 0.1f, 0);
    }

    private void ApplyLockedOnAnimationParameter(PlayerMode mode)
    {
        _animator.SetBool(AnimParams.IsLockedOn, _isLockedOn && mode != PlayerMode.Thunder);
    }
}
