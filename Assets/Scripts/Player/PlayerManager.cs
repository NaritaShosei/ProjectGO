using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerManager : MonoBehaviour, ICharacter
{
    [Header("コンポーネント設定")]
    [SerializeField] private PlayerMove _move;
    [SerializeField] private PlayerAttacker _attacker;
    [SerializeField] private InputHandler _input;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _targetTransform;
    [Header("データ")]
    [SerializeField] private CharacterData _data;
    [Header("ダメージリアクションの閾値")]
    [SerializeField] private float _damageThreshold = 10;

    private PlayerStateFlags _flags;
    private PlayerStats _stats;
    private CancellationTokenSource _cts;

    public Camera MainCamera { get; private set; }


    // 状態の遷移条件
    public bool CanAttack => !HasFlag(PlayerStateFlags.Dead | PlayerStateFlags.MoveLocked | PlayerStateFlags.Dodging | PlayerStateFlags.Charging);
    public bool CanStartCharge => !HasFlag(PlayerStateFlags.Dead | PlayerStateFlags.MoveLocked | PlayerStateFlags.Dodging | PlayerStateFlags.Attacking);
    public bool IsCharging => HasFlag(PlayerStateFlags.Charging);
    public bool CanMove => !HasFlag(PlayerStateFlags.MoveLocked | PlayerStateFlags.Dodging | PlayerStateFlags.Dead);
    public bool TryDodge(float staminaCost)
    {
        if (HasFlag(PlayerStateFlags.Dodging | PlayerStateFlags.DodgeLocked | PlayerStateFlags.Dead))
            return false;

        return _stats.TryUseStamina(staminaCost);
    }
    public event Action OnDead;

    private void Awake()
    {
        _move.Init(this, _input, _animator, _data);
        _attacker.Init(this, _input, _animator);
        _stats = new PlayerStats(_data);
        MainCamera = Camera.main;
    }

    private void Update()
    {
        if (!HasFlag(PlayerStateFlags.Dead))
        {
            _stats.UpdateStaminaRegeneration(_data.StaminaRegenRate);
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

    private void Dead()
    {
        Debug.Log("DEAD");
        AddFlags(PlayerStateFlags.Dead | PlayerStateFlags.MoveLocked | PlayerStateFlags.DodgeLocked);
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
        return _targetTransform;
    }

    private void CancelAndDisposeCTS()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
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
}