namespace StateMachine.Interfaces;

public interface IStateContext<TState> where TState : struct, Enum
{
    TState CurrentState { get; }
    bool HasValue(string key);
    T GetValue<T>(RecordKey<TState, T> recordKey) where T : notnull;
    bool TryGetValue<T>(RecordKey<TState, T> recordKey, out T value) where T : notnull;
    IStateRecord<TValue> GetStateRecord<TValue>(RecordKey<TState, TValue> recordKey) where TValue : notnull;
    void SetValue<T>(RecordKey<TState, T> recordKey, T value) where T : notnull;
    bool TryMoveNext(out string[] missingKey);
    bool CheckInitialized(out string[] missingKeys);
}