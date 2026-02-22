using System;

public class EnemyStateContext
{
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    // 状態変更イベント(必要に応じて)
    public event Action<EnemyState, EnemyState> OnStateChanged;

    public float DurationElectrifiedTime { get; private set; } 

    public bool IsAttacking => CurrentState is EnemyState.Attack;

    public bool IsBarking => CurrentState is EnemyState.Bark;

    public bool IsMoving => CurrentState is EnemyState.Move;

    public bool IsElectrified => CurrentState is EnemyState.Electrified;

    public bool IsDead => CurrentState is EnemyState.Dead;

    public void ChangeState(EnemyState newState)
    {
        if (CurrentState == newState) return;

        EnemyState oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(oldState, newState);
    }

    public bool CanAttack() => CurrentState is EnemyState.Idle
                            || CurrentState is EnemyState.Move;
    public bool CanMove() => CurrentState != EnemyState.Attack
                          && CurrentState != EnemyState.Bark
                          && CurrentState != EnemyState.Electrified
                          && CurrentState != EnemyState.NockBack
                          && CurrentState != EnemyState.Dead;

    public void SetElectrifiedTime(float durationTime)
    {
        DurationElectrifiedTime = durationTime;
    }
}
public enum EnemyState
{
    Idle,
    Attack,
    Bark,
    Move,
    Electrified,     
    NockBack,   
    Down,
    Dead,
}
