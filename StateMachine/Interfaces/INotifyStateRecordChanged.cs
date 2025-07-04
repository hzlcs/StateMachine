namespace StateMachine.Interfaces;

public interface INotifyStateRecordChanged
{
    event StateRecordChangedHandler? StateRecordChanged;
}

public delegate void StateRecordChangedHandler(string key, string propertyName, object? before, object? after);