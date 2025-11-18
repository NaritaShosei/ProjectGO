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

    private PlayerStats _stats;
    private CancellationTokenSource _cts;

    public Camera MainCamera { get; private set; }

    public PlayerState CurrentState { get; private set; }

    // 状態の遷移条件
    public bool CanAttack => CurrentState == PlayerState.None;
    public bool CanStartCharge => CurrentState == PlayerState.None;
    public bool IsCharging => CurrentState == PlayerState.Charge;
    public bool CanMove => CurrentState switch
    {
        PlayerState.Dodge => false,
        PlayerState.Attack => false,
        PlayerState.Dead => false,
        PlayerState.Invincible => false,
        _ => true
    };
    public bool CanDodge => CurrentState switch
    {
        PlayerState.Dodge => false,
        PlayerState.Attack => false,
        PlayerState.Dead => false,
        PlayerState.Invincible => false,
        _ => true
    };

    public event Action OnDead;

    private void Awake()
    {
        _move.Init(this, _input, _animator, _data);
        _attacker.Init(this, _input, _animator);
        _stats = new PlayerStats(_data);
        MainCamera = Camera.main;
    }

    #region 状態遷移

    private void ChangeState(PlayerState state)
    {
        if (CurrentState == PlayerState.Dead)
        {
            Debug.Log("死亡済みのためステートを変更できません"); return;
        }

        if (state == CurrentState)
        {
            Debug.Log("ステートに変化がありません"); return;
        }

        CurrentState = state;
    }

    public void StartAttack()
    {
        ChangeState(PlayerState.Attack);
    }

    public void StartCharge()
    {
        ChangeState(PlayerState.Charge);
    }

    public void StartDodge()
    {
        ChangeState(PlayerState.Dodge);
    }

    public void EndAction()
    {
        ChangeState(PlayerState.None);
    }
    #endregion

    public async void AddDamage(float damage)
    {
        if (CurrentState == PlayerState.Dead)
        {
            Debug.Log("死亡しているためダメージを受けません");
            return;
        }

        if (CurrentState == PlayerState.Invincible)
        {
            Debug.Log("無敵中のためダメージを受けません");
            return;
        }

        CancelAndDisposeCTS();

        if (!_stats.TryAddDamage(damage))
        {
            Debug.Log("DEAD");
            ChangeState(PlayerState.Dead);
            OnDead?.Invoke();
            return;
        }

        string clipName = _damageThreshold <= damage ? _data.BigHitClip.name : _data.SmallHitClip.name;

        _animator.Play(clipName);

        // ダメージを受けた際に攻撃をキャンセル
        _attacker.CancelAttack();

        // 状態をリセット
        EndAction();

        ChangeState(PlayerState.Invincible);

        _cts = new CancellationTokenSource();

        try
        {
            await CancelInvincible(_cts.Token);
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
            // 念のため
            EndAction();
        }
    }

    private async UniTask CancelInvincible(CancellationToken token)
    {
        float delay = _data.HitStopDuration;

        if (_data.SmallHitClip != null)
        {
            delay += _data.SmallHitClip.length;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(delay), false, PlayerLoopTiming.Update, token);

        EndAction();
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

public enum PlayerState
{
    None,
    Attack,
    Charge,
    Dodge,
    Dead,
    Invincible,
}