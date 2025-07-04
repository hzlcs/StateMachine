using System.Collections.ObjectModel;
using System.Diagnostics;
using StateMachine.Interfaces;

namespace StateMachine;

internal class StateMachine<TState> : IStateMachine<TState> where TState : struct, Enum
{
    // ReSharper disable once StaticMemberInGenericType
    private static ulong _idCounter = 0;
    private static readonly TState[] AllStates = Enum.GetValues<TState>();
    private readonly string name;
    private readonly StateContext<TState> stateContext;
    private readonly ReadOnlyDictionary<TState, IOperation<TState>> operations;

    internal StateMachine(string name, StateContext<TState> stateContext, Dictionary<TState, IOperation<TState>>  operations)
    {
        this.name = name;
        this.stateContext = stateContext;
        this.operations = new ReadOnlyDictionary<TState, IOperation<TState>>(operations);
        CurrentState = AllStates[0]; // Initialize to the first state
    }

    internal StateMachine(string name, StateContext<TState> stateContext, ReadOnlyDictionary<TState, IOperation<TState>> operations)
    {
        this.name = name;
        this.stateContext = stateContext;
        this.operations = operations;
        CurrentState = AllStates[0]; // Initialize to the first state
    }

    public ulong Id { get; } = Interlocked.Increment(ref _idCounter);
    public TState CurrentState { get; private set; }
    public ResultCode MoveNext(out string errorMessage)
    {
        var operation = operations[CurrentState];
        var runResult = operation.Run(stateContext, out errorMessage);
        if(runResult != ResultCode.Success)
        {
            if (runResult == ResultCode.Retry)
            {
                return ResultCode.Retry;
            }
            errorMessage = $"Operation '{operation.State}' failed with error: {errorMessage}";
            // Log buffer.ToJson();
            return ResultCode.Error;
        }
        if (!stateContext.TryMoveNext(out var missingKeys))
        {
            errorMessage = $"Buffer failed to move to the next state. Missing keys: {string.Join(", ", missingKeys)}";
            return ResultCode.Error;
        }
        CurrentState = stateContext.CurrentState;
        errorMessage = string.Empty;
        return ResultCode.Success;

    }

    public void InitState<TValue>(RecordKey<TState, TValue> recordKey, TValue value) where TValue : notnull
    {
        stateContext.SetValue(recordKey, value);
    }

    public bool Initialize(out string errorMessage)
    {
        errorMessage = string.Empty;

        foreach (var state in AllStates)
        {
            if (operations.ContainsKey(state)) 
                continue;
            errorMessage = $"No operation defined for state '{state}'.";
            return false;
        }
        if(!stateContext.CheckInitialized(out var missingKeys))
        {
            errorMessage = $"State context is not initialized. Missing keys: {string.Join(", ", missingKeys)}";
            return false;
        }
        foreach (var operation in operations)
        {
            if (operation.Value.CheckInitialized(stateContext, out missingKeys)) 
                continue;
            errorMessage = $"Operation '{operation.Key}' initialized failed, follow keys need to be initialized: {string.Join(", ", missingKeys)}";
            return false;
        }
        return true;
    }

    public void Reset()
    {
        CurrentState = AllStates[0]; // Reset to the first state
        stateContext.Reset();
    }

    public IStateMachine<TState> Fork(bool reset = true)
    {
        var newBuffer = stateContext.Fork(reset);
        var newStateMachine = new StateMachine<TState>(name, newBuffer, operations)
        {
            CurrentState = CurrentState // Preserve the current state
        };
        if (reset)
        {
            newStateMachine.Reset();
        }
        return newStateMachine;
    }
}