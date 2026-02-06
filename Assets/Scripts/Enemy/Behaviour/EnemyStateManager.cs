using System;

public class EnemyStateManager
{
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    // 状態変更イベント(必要に応じて)
    public event Action<EnemyState, EnemyState> OnStateChanged;

    public bool IsAttacking => CurrentState is EnemyState.Attacking;

    public bool IsBarking => CurrentState is EnemyState.Barking;

    public bool IsMoving => CurrentState is EnemyState.Moving;

    public bool IsStun => CurrentState is EnemyState.Stun;

    public bool IsShock => CurrentState is EnemyState.Shock;

    public bool IsDead => CurrentState is EnemyState.Dead;

    public void ChangeState(EnemyState newState)
    {
        if (CurrentState == newState) return;

        EnemyState oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(oldState, newState);
    }

    public bool CanAttack() => CurrentState is EnemyState.Idle
                            || CurrentState is EnemyState.Moving;
    public bool CanMove() => CurrentState != EnemyState.Attacking
                          && CurrentState != EnemyState.Barking
                          && CurrentState != EnemyState.Stun
                          && CurrentState != EnemyState.Shock
                          && CurrentState != EnemyState.NockBack
                          && CurrentState != EnemyState.Dead;
    
    // TODO: 削除するかも。今のところ使用予定はない
    public bool CanModeChange() => CurrentState is EnemyState.Idle;

}
public enum EnemyState
{
    // TODO: "ing"ありなしを統一するべき
    Idle,
    Attacking,
    Barking,
    Moving,
    Stun,       // 物理攻撃による気絶
    Shock,      // 感電攻撃による感電
    // TODO: NockBackかつ感電状態を想定するなら廃止するべき
    // TODO: NockBack後にStun, Shockを開始するならあり
    NockBack,   
    Dead,
}