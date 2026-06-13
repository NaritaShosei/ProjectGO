using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// インゲームのフェーズ遷移を管理するStateMachine。
/// SequenceManagerが保有し、毎フレームTickを呼ぶ。
/// Stateリストは固定長ではなく外部から登録する。
/// </summary>
public class SequenceStateMachine
{
    public SequenceStateType CurrentStateType => _current?.StateType ?? SequenceStateType.IntroMovie;

    public SequenceStateMachine(SequenceStateContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Stateを登録する。同じTypeを2回登録すると上書きされる。
    /// </summary>
    public void RegisterState(ISequenceState state)
    {
        _states[state.StateType] = state;
    }

    /// <summary>
    /// 指定のStateで開始する。
    /// </summary>
    public void Start(SequenceStateType initialState)
    {
        TransitionTo(initialState);
    }

    /// <summary>
    /// 毎フレーム呼ぶ。現在のStateをTickし、遷移先が返ってきたら切り替える。
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (_current == null) return;

        var next = _current.Tick(_context, deltaTime);
        if (next.HasValue)
        {
            TransitionTo(next.Value);
        }
    }

    /// <summary>
    /// 外部から強制的にStateを切り替える（死亡など割り込み用）。
    /// </summary>
    public void ForceTransition(SequenceStateType nextState)
    {
        TransitionTo(nextState);
    }

    private readonly SequenceStateContext _context;
    private readonly Dictionary<SequenceStateType, ISequenceState> _states = new();
    private ISequenceState _current;

    private void TransitionTo(SequenceStateType nextType)
    {
        if (_current != null && _current.StateType == nextType)
        {
            return;
        }

        if (!_states.TryGetValue(nextType, out var next))
        {
            Debug.LogWarning($"[SequenceStateMachine] State '{nextType}' が登録されていません。");
            return;
        }

        _current?.OnExit(_context);
        _context.ResetTransitionFlags();

        _current = next;
        _current.OnEnter(_context);

        Debug.Log($"[SequenceStateMachine] → {nextType}");
    }
}
