using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerManager : MonoBehaviour, IPlayer, IHealth, IStamina
{
    [Header("コンポーネント設定")]
    [SerializeField] private PlayerMove _move;
    [SerializeField] private PlayerAttacker _attacker;
    [SerializeField] private PlayerModeManager _modeManager;
    [SerializeField] private Interactor _interactor;
    [SerializeField] private InputHandler _input;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _targetCenter;
    [SerializeField] private PlayerUIManager _playerUIManager;
    [SerializeField] private SpeedManager _speedManager;
    [Header("データ")]
    [SerializeField] private CharacterData _data;
    [Header("ダメージリアクションの閾値")]
    [SerializeField] private float _damageThreshold = 10;

    private PlayerStateFlags _flags;
    private PlayerModeType _modeType;

    private PlayerStats _stats;
    private PlayerAnimator _anim;
    private CancellationTokenSource _cts;

    public Camera MainCamera { get; private set; }


    // 状態の遷移条件
    public bool CanAttack => !HasFlag(PlayerStateFlags.Dead | PlayerStateFlags.MoveLocked | PlayerStateFlags.Dodging | PlayerStateFlags.Charging);
    public bool CanStartCharge => !HasFlag(PlayerStateFlags.Dead | PlayerStateFlags.MoveLocked | PlayerStateFlags.Dodging | PlayerStateFlags.Attacking | PlayerStateFlags.Charging);
    public bool IsCharging => HasFlag(PlayerStateFlags.Charging);
    public bool CanMove => !HasFlag(PlayerStateFlags.MoveLocked | PlayerStateFlags.Dodging | PlayerStateFlags.Dead);
    public bool CanDodgeAttack => HasFlag(PlayerStateFlags.CanDodgeAttack) && !HasFlag(PlayerStateFlags.Dead | PlayerStateFlags.MoveLocked | PlayerStateFlags.Dodging | PlayerStateFlags.Charging | PlayerStateFlags.Attacking);

    public bool TryDodge(float staminaCost)
    {
        if (HasFlag(PlayerStateFlags.Dodging | PlayerStateFlags.DodgeLocked | PlayerStateFlags.Dead))
            return false;

        return _stats.TryUseStamina(staminaCost);
    }

    public event Action OnDead;
    public event Action OnDodge;
    public event Action<PlayerModeType> OnModeChange;

    private void Awake()
    {
        MainCamera = Camera.main;

        _move.Init(this, _input, _animator, _data);
        _attacker.Init(this, _input, _animator);
        _interactor.Init(_input);
        _modeManager.Init(this, _attacker, _input);

        _anim = new PlayerAnimator(_speedManager, _animator);
        _stats = new PlayerStats(_data);

        if (_playerUIManager == null)
        {
            Debug.LogWarning("PlayerUIManagerが設定されていません", this);
            return;
        }

        _stats.OnHealthChange += _playerUIManager.HPGauge.UpdateGauge;
        _stats.OnStaminaChange += _playerUIManager.StaminaGauge.UpdateGauge;

        // ゲージの初期値を設定
        _playerUIManager.HPGauge.Init(_data.MaxHP, _data.MaxHP);
        _playerUIManager.StaminaGauge.Init(_data.MaxStamina, _data.MaxStamina);

        OnModeChange += _playerUIManager.ModeView.UpdateView;
        _playerUIManager.ModeView.UpdateView(_modeType);
    }

    private void Update()
    {
        if (!HasFlag(PlayerStateFlags.Dead) && !HasFlag(PlayerStateFlags.Dodging) && !IsModeType(PlayerModeType.Thunder))
        {
            _stats.UpdateStaminaRegeneration(_data.StaminaRegenRate);
        }
    }

    private void OnDestroy()
    {
        if (_stats != null && _playerUIManager != null)
        {
            _stats.OnHealthChange -= _playerUIManager.HPGauge.UpdateGauge;
            _stats.OnStaminaChange -= _playerUIManager.StaminaGauge.UpdateGauge;
            OnModeChange -= _playerUIManager.ModeView.UpdateView;
        }
    }

    #region 状態遷移

    /// <summary>
    /// 状態を追加
    /// </summary>
    public void AddFlags(PlayerStateFlags flags)
    {
        _flags |= flags;
        Debug.Log($"[状態追加] 現在: {_flags}");
    }

    /// <summary>
    /// 状態を削除
    /// </summary>
    public void RemoveFlags(PlayerStateFlags flags)
    {
        _flags &= ~flags;
        Debug.Log($"[状態削除] 現在: {_flags}");
    }

    /// <summary>
    /// 指定した状態のいずれかを持っているか
    /// </summary>
    public bool HasFlag(PlayerStateFlags flags)
    {
        return (_flags & flags) != 0;
    }

    public void ModeChange(PlayerModeType mode)
    {
        if (_modeType == mode)
        {
            Debug.Log($"{_modeType}から{mode}に切り替えようとしています。かわりません。");
            return;
        }

        _modeType = mode;

        // UIなどに通知
        OnModeChange?.Invoke(_modeType);
    }

    /// <summary>
    /// 指定した状態かどうかを返す
    /// </summary>
    public bool IsModeType(PlayerModeType mode)
    {
        return _modeType == mode;
    }

    #endregion

    public async void AddDamage(float damage)
    {
        if (HasFlag(PlayerStateFlags.Dead))
        {
            Debug.Log("死亡しているためダメージを受けません");
            return;
        }

        if (HasFlag(PlayerStateFlags.Invincible))
        {
            Debug.Log("無敵中のためダメージを受けません");
            return;
        }

        CancelAndDisposeCTS();

        if (!_stats.TryAddDamage(damage))
        {
            Dead();
            return;
        }

        AnimationClip selectedClip = _damageThreshold <= damage ? _data.BigHitClip : _data.SmallHitClip;

        if (selectedClip == null)
        {
            Debug.LogError($"{damage}ダメージに対応するダメージリアクションアニメーションがアサインされていません");
            return;
        }
        _animator.Play(selectedClip.name);

        // ダメージを受けた際に攻撃をキャンセル
        _attacker.CancelAttack();

        AddFlags(PlayerStateFlags.MoveLocked | PlayerStateFlags.DodgeLocked | PlayerStateFlags.Invincible);

        _cts = new CancellationTokenSource();

        try
        {
            await CancelInvincible(selectedClip, _cts.Token);
        }

        catch (OperationCanceledException)
        {
            // 正常キャンセル
        }

        catch (Exception ex)
        {
            Debug.LogError($"無敵時間でエラー{ex}");
        }

        finally
        {
            RemoveFlags(PlayerStateFlags.Invincible | PlayerStateFlags.MoveLocked | PlayerStateFlags.DodgeLocked);
        }
    }

    public void Healing(float amount)
    {
        _stats.RecoverHp(amount);
    }

    public void OnDodgeInvoke()
    {
        OnDodge?.Invoke();
    }
    private void Dead()
    {
        Debug.Log("DEAD");
        AddFlags(PlayerStateFlags.Dead | PlayerStateFlags.MoveLocked | PlayerStateFlags.DodgeLocked);

        _animator.Play("Dead");

        OnDead?.Invoke();
    }

    private async UniTask CancelInvincible(AnimationClip hitClip, CancellationToken token)
    {
        float delay = _data.HitStopDuration;

        if (hitClip != null)
        {
            delay += hitClip.length;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(delay), false, PlayerLoopTiming.Update, token);
    }
    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    private void CancelAndDisposeCTS()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    public bool TryUseStamina(float amount)
    {
        return _stats.TryUseStamina(amount);
    }

    public void SetCharacterData(CharacterData data)
    {
        _data = data;
        _move.SetCharacterData(data);
    }
}

/// <summary>
/// プレイヤーの状態フラグ（複数同時に持てる）
/// </summary>
[Flags]
public enum PlayerStateFlags
{
    None = 0,
    Attacking = 1 << 0,   // 攻撃中
    Dodging = 1 << 1,   // 回避中
    Invincible = 1 << 2,   // 無敵
    MoveLocked = 1 << 3,   // 移動不能
    Dead = 1 << 4,   // 死亡
    Charging = 1 << 5,   // チャージ中
    DodgeLocked = 1 << 6, // 回避不能
    CanDodgeAttack = 1 << 7, // 回避攻撃に派生可能
    CanHeavyCombo = 1 << 8, // 強攻撃からコンボ派生可能
    ModeChange = 1 << 9, // モードチェンジ中
}

public enum PlayerModeType
{
    Battle,
    Thunder,
}