using System;

public class PlayerStateManager
{
    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    // 状態変更イベント(必要に応じて)
    public event Action<PlayerState, PlayerState> OnStateChanged;

    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;

        PlayerState oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(oldState, newState);
    }

    public bool CanAttack() => CurrentState is PlayerState.Idle or PlayerState.Moving;
    public bool CanMove() => CurrentState != PlayerState.Attacking
                          && CurrentState != PlayerState.Dodge
                          && CurrentState != PlayerState.Damaged
                          && CurrentState != PlayerState.Dead;
    public bool CanDodge() => CurrentState is PlayerState.Idle or PlayerState.Moving;
}   
public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
    Dodge,
    Damaged,
    Dead,
}