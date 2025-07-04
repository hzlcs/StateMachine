namespace StateMachine.Interfaces;

public interface IStateMachine<TState> where TState : struct, Enum
{
    ulong Id { get; }
    TState CurrentState { get; }
    ResultCode MoveNext(out string errorMessage);
    void InitState<TValue>(RecordKey<TState, TValue> recordKey, TValue value) where TValue : notnull;
    bool Initialize(out string errorMessage);
    void Reset();
    IStateMachine<TState> Fork(bool reset = true);
}