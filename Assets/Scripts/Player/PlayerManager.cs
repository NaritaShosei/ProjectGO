using System;
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

    private PlayerStats _stats;

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
        _ => true
    };
    public bool CanDodge => CurrentState switch
    {
        PlayerState.Dodge => false,
        PlayerState.Attack => false,
        PlayerState.Dead => false,
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

    #region 状態遷移

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

    public void AddDamage(float damage)
    {
        if (CurrentState == PlayerState.Dead) { return; }

        if (!_stats.TryAddDamage(damage))
        {
            Debug.Log("DEAD");
            ChangeState(PlayerState.Dead);
            OnDead?.Invoke();
            return;
        }

        // ダメージを受けた際に攻撃をキャンセル
        _attacker.CancelAttack();
        EndAction();
    }

    public Transform GetTargetCenter()
    {
        return _targetTransform;
    }
    #endregion
}

public enum PlayerState
{
    None,
    Attack,
    Charge,
    Dodge,
    Dead,
}