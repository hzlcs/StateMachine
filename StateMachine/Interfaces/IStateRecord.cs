namespace StateMachine.Interfaces;



internal interface IStateRecord
{
    string Key { get; }
    bool HasValue { get; }
    object Value { get; }
    Enum? DependState { get; }
    void Reset();
    IStateRecord Clone();
}

public interface IStateRecord<out T> where T : notnull 
{
    string Key { get; }
    bool HasValue { get; }
    T Value { get; }
}

public record RecordKey<TState, TValue>(string Key, TState? DependState = null) where TValue : notnull where TState : struct, Enum;
