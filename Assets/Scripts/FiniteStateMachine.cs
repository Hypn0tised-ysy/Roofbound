using System;
using System.Collections.Generic;

/// <summary>
/// 轻量泛型状态机：负责状态保存与切换通知，不耦合具体业务逻辑。
/// </summary>
public sealed class FiniteStateMachine<TState> where TState : struct, Enum
{
    public TState CurrentState { get; private set; }
    public bool IsInitialized { get; private set; }

    public event Action<TState, TState> OnStateChanged;

    public void Initialize(TState initialState)
    {
        CurrentState = initialState;
        IsInitialized = true;
    }

    public bool ChangeState(TState nextState)
    {
        if (!IsInitialized)
        {
            Initialize(nextState);
            return true;
        }

        if (EqualityComparer<TState>.Default.Equals(CurrentState, nextState))
        {
            return false;
        }

        TState previous = CurrentState;
        CurrentState = nextState;
        OnStateChanged?.Invoke(previous, nextState);
        return true;
    }
}
