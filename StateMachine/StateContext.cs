using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StateMachine.Interfaces;

namespace StateMachine;

public class StateContext<TState> : IStateContext<TState> where TState : struct, Enum
{
    private ulong dataVersion;

    private ulong loopVersion = 1;

    internal StateContext()
    {
        allStates = Enum.GetValues<TState>();
        if (allStates.Length == 0)
        {
            throw new InvalidOperationException("No states defined in the enum.");
        }

        CurrentState = allStates[0];

        // Initialize state buffer
        foreach (var state in allStates)
        {
            stateDict[state] = [];
        }
    }

    private readonly TState[] allStates;

    public TState CurrentState { get; private set; }

    private readonly Dictionary<string, IStateRecord> allStateRecord = [];

    private readonly Dictionary<string, IStateRecord> initStateRecord = [];

    private readonly Dictionary<TState, List<IStateRecord>> stateDict = [];

    public T GetValue<T>(RecordKey<TState, T> recordKey) where T : notnull
    {
        if (!TryGetValue(recordKey, out var value))
            throw new StateException($"Value for key '{recordKey.Key}' is not set.");
        return value;
    }

    public bool TryGetValue<T>(RecordKey<TState, T> recordKey, out T value) where T : notnull
    {
        var record = GetStateRecordOrThrow<T>(recordKey.Key);
        if (record.HasValue)
        {
            value = record.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool HasValue(string key)
    {
        return GetStateRecordOrThrow(key).HasValue;
    }

    public void SetValue<T>(RecordKey<TState, T> recordKey, T value) where T : notnull
    {
        if (recordKey.DependState.HasValue)
        {
            if (!CurrentState.Equals(recordKey.DependState.Value))
                throw new StateException(
                    $"Error when set value: Record '{recordKey.Key}' depends on state '{recordKey.DependState.Value}', " +
                    $"but current state is '{CurrentState}'.");
        }

        InternalSetValue(recordKey.Key, value);
    }

    private void InternalSetValue<T>(string key, T value) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        InternalSetValue(GetStateRecordOrThrow<T>(key), value);
    }

    private void InternalSetValue<T>(StateRecord<T> stateRecord, T value) where T : notnull
    {
        if (stateRecord.HasValue)
        {
            throw new StateException($"Value for key '{stateRecord.Key}' is already set.");
        }

        if (value is INotifyStateRecordChanged notifyStateRecord)
        {
            notifyStateRecord.StateRecordChanged += OnStateRecordPropertyChanged;
        }

        // If the value is not INotifyBufferChanged, we still log the change
        OnStateRecordInitialized(stateRecord.Key, value);
        stateRecord.SetValue(value);
    }

    private void OnStateRecordInitialized(string key, object value)
    {
        ++dataVersion;
        Debug.WriteLine(
            $"StateRecord initialized on {loopVersion}:'{CurrentState}'-{dataVersion}\n" +
            $"Key='{key}', Value='{GetLogString(value)}'");
    }

    private void OnStateRecordPropertyChanged(string key, string propertyName, object? before, object? after)
    {
        ++dataVersion;
        Debug.WriteLine(
            $"StateRecord property changed on {loopVersion}:'{CurrentState}'-{dataVersion}\n" +
            $"Key='{key}', property='{propertyName}', Before='{GetLogString(before)}', After='{GetLogString(after)}'");
    }

    private static string GetLogString(object? value)
    {
        if (value is null)
            return "NULL";
        if (value is DateTime dateTime)
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        if (value.GetType().IsClass)
            return JsonConvert.SerializeObject(value);
        return value.ToString() ?? string.Empty;
    }

    public bool TryMoveNext(out string[] missingKeys)
    {
        missingKeys = [..stateDict[CurrentState].Where(v => !v.HasValue).Select(v => v.Key)];
        if (missingKeys.Length > 0)
        {
            return false; // Missing data for the current state
        }

        // All data is set, move to the next state
        var nextStateIndex = (Convert.ToInt32(CurrentState) + 1) % allStates.Length;
        if (nextStateIndex == 0)
        {
            Reset();
        }
        else
        {
            CurrentState = allStates[nextStateIndex];
        }

        return true; // Successfully moved to the next state
    }

    public bool CheckInitialized(out string[] missingKeys)
    {
        missingKeys = [.. initStateRecord.Values.Where(v => !v.HasValue).Select(v => v.Key)];
        return missingKeys.Length == 0;
    }

    public IStateRecord<TValue> GetStateRecord<TValue>(RecordKey<TState, TValue> recordKey) where TValue : notnull
    {
        return GetStateRecordOrThrow<TValue>(recordKey.Key);
    }

    internal void AddStateRecord<TValue>(RecordKey<TState, TValue> record) where TValue : notnull
    {
        var (key, dependState) = record;
        if (allStateRecord.ContainsKey(key))
        {
            throw new StateException($"Data with key '{key}' already exists in the buffer.");
        }
        var stateRecord = new StateRecord<TValue>(key, dependState);
        if (dependState.HasValue)
        {
            stateDict[dependState.Value].Add(stateRecord);
        }
        else
        {
            initStateRecord.Add(key, stateRecord);
        }

        allStateRecord.Add(key, stateRecord);
    }

    private IStateRecord GetStateRecordOrThrow(string key)
    {
        if (!allStateRecord.TryGetValue(key, out var data))
        {
            throw new StateException($"No data found with key '{key}'.");
        }

        return data;
    }

    private StateRecord<TValue> GetStateRecordOrThrow<TValue>(string key) where TValue : notnull
    {
        var data = GetStateRecordOrThrow(key);
        if (data is not StateRecord<TValue> bufferData)
            throw new StateException($"data with key '{key}' is not of type '{typeof(TValue)}'.");
        return bufferData;
    }

    internal void Reset()
    {
        CurrentState = allStates[0];
        // Reset all data in the state buffer because dataBuffer may have initialized values
        foreach (var data in stateDict.Values.SelectMany(v => v))
        {
            if (data is { HasValue: true, Value: INotifyStateRecordChanged notifyStateRecord })
            {
                notifyStateRecord.StateRecordChanged -= OnStateRecordPropertyChanged;
            }

            data.Reset();
        }

        ++loopVersion;
    }

    internal string ToJson()
    {
        var array = new JArray();
        foreach (var data in allStateRecord.Values)
        {
            var obj = new JObject
            {
                [nameof(IStateRecord.Key)] = data.Key,
                [nameof(IStateRecord.DependState)] = data.DependState?.ToString() ?? "null",
                [nameof(IStateRecord.Value)] = JsonConvert.SerializeObject(data.Value),
            };
            array.Add(obj);
        }

        return array.ToString(Formatting.Indented);
    }

    internal void LoadJson(string json)
    {
        var array = JArray.Parse(json);
        foreach (var item in array)
        {
            var key = item[nameof(IStateRecord.Key)]?.ToString() ?? throw new StateException("Key is missing in JSON.");
            if (!allStateRecord.TryGetValue(key, out var record))
            {
                throw new StateException($"buffer does not contain key '{key}' to load from JSON.");
            }

            var dependStateStr = item[nameof(IStateRecord.DependState)]?.ToString();
            if (dependStateStr is null or "null")
            {
                if (record.DependState is not null)
                    throw new StateException($"dependState is missing in JSON.");
            }
            else if (Enum.TryParse<TState>(dependStateStr, out var dependState))
            {
                if (record.DependState is null || !record.DependState.Equals(dependState))
                {
                    throw new StateException(
                        $"Depend state mismatch for key '{key}'. Expected: {record.DependState}, Found: {dependState}");
                }
            }
            else
            {
                throw new StateException($"Invalid depend state '{dependStateStr}' for key '{key}'.");
            }

            var value = item[nameof(IStateRecord.Value)]?.ToString();
            if (value is not null)
            {
                var dataValue = JsonConvert.DeserializeObject(value, record.GetType().GetGenericArguments()[0]);
                if (dataValue is not null)
                    InternalSetValue(key, dataValue);
            }
        }
    }

    public StateContext<TState> Fork(bool reset)
    {
        var newStateContext = new StateContext<TState>();
        foreach (var (key, value) in allStateRecord)
        {
            var dependState = value.DependState;

            var newRecord = value.Clone();
            if (reset)
            {
                newRecord.Reset();
            }

            if (dependState is null)
            {
                newStateContext.initStateRecord.Add(key, newRecord);
            }
            else
            {
                newStateContext.stateDict[(TState)dependState].Add(newRecord);
            }

            newStateContext.allStateRecord.Add(key, newRecord);
        }

        newStateContext.CurrentState = reset ? allStates[0] : CurrentState;
        return newStateContext;
    }
}