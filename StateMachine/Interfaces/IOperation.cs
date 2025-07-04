namespace StateMachine.Interfaces;

public interface IOperation<TState> where TState : struct, Enum
{
    TState State { get; }
    ResultCode Run(IStateContext<TState> stateContext, out string errorMessage);
    bool CheckInitialized(IStateContext<TState> stateContext, out string[] missingKeys);
}