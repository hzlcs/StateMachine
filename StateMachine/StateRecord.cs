using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StateMachine.Interfaces;

namespace StateMachine;



public class StateRecord<TValue>(string key, Enum? dependState) : IStateRecord, IStateRecord<TValue>
    where TValue : notnull
{
    private TValue? _value;
    public string Key { get; } = key;
    public Enum? DependState { get; } = dependState;
    void IStateRecord.Reset()
    {
        Reset();
    }

    IStateRecord IStateRecord.Clone()
    {
        return (IStateRecord)MemberwiseClone();
    }

    public bool HasValue { get; private set; }
    object IStateRecord.Value => GetValue();

    internal void SetValue(TValue value)
    {
        HasValue = true;
        _value = value;
    }

    internal void Reset()
    {
        HasValue = false;
        _value = default;
    }

    public TValue Value => GetValue();

    private TValue GetValue()
    {
        if (HasValue && _value is not null)
        {
            return _value;
        }

        throw new StateException($"Value of '{Key}' is not set.");
    }
}