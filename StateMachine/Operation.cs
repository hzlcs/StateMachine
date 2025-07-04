using StateMachine.Interfaces;

namespace StateMachine;

public abstract class Operation<TState> : IOperation<TState>
    where TState : struct, Enum
{
    public abstract TState State { get; }
    
    public virtual ResultCode Run(IStateContext<TState> stateContext, out string errorMessage)
    {
        errorMessage = string.Empty;
        return default;
    }

    public virtual bool CheckInitialized(IStateContext<TState> stateContext, out string[] missingKeys)
    {
        missingKeys = [];
        return true;
    }

}