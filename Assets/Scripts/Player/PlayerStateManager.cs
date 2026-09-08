using System;

public class PlayerStateManager
{
    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    public InvincibleType CurrentInvincibleType { get; private set; } = InvincibleType.None;

    public event Action<PlayerState, PlayerState> OnStateChanged;

    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;

        PlayerState oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(oldState, newState);
    }

    public bool CanAttack() => CurrentState is PlayerState.Idle;

    public bool CanMove() => CurrentState != PlayerState.Attacking
                          && CurrentState != PlayerState.Dodge
                          && CurrentState != PlayerState.Charging
                          && CurrentState != PlayerState.Damaged
                          && CurrentState != PlayerState.Down
                          && CurrentState != PlayerState.ModeChanging
                          && CurrentState != PlayerState.Dead;

    /// <summary>
    /// 回避可能かどうか。
    /// Idle・Attacking状態から回避できる（攻撃の中断が可能）。
    /// Damaged・ModeChanging・Dodge・Deadは不可。
    /// </summary>
    public bool CanDodge() => CurrentState is PlayerState.Idle
                           || CurrentState is PlayerState.Attacking
                           || CurrentState is PlayerState.Charging;

    public bool IsDodging() => CurrentState is PlayerState.Dodge;

    public bool IsCharging() => CurrentState is PlayerState.Charging;

    public bool IsDown() => CurrentState is PlayerState.Down;

    public bool IsDead() => CurrentState is PlayerState.Dead;

    public bool IsDamaged() => CurrentState is PlayerState.Damaged;

    public bool CanModeChange() => CurrentState is PlayerState.Idle;

    public bool CanInteract() => CurrentState is PlayerState.Idle;

    public bool IsInvincible()
    {
        return CurrentInvincibleType != InvincibleType.None;
    }

    public void AddInvincible(InvincibleType type)
    {
        CurrentInvincibleType |= type;
    }

    public void RemoveInvincible(InvincibleType type)
    {
        CurrentInvincibleType &= ~type;
    }
}

public enum PlayerState
{
    Idle,
    Attacking,
    Charging,
    Dodge,
    Damaged,
    Down,
    Dead,
    ModeChanging
}

/// <summary>
/// 無敵の種類。複数の無敵状態を同時に管理するためにフラグ列挙体で定義する。
/// </summary>
[Flags]
public enum InvincibleType
{
    None = 0, // 0000
    Dodge = 1 << 0, // 0001
    Damaged = 1 << 1, // 0010
}
