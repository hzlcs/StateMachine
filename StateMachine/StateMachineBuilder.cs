using System.Collections.ObjectModel;
using System.Numerics;
using System.Reflection;
using StateMachine.Interfaces;

namespace StateMachine;

public static class StateMachineBuilder
{
    public static IStateMachine<TState> Create<TState>() where TState : struct, Enum
    {
        return StateMachineBuilder<TState>.Create();
    }
}

public static class StateMachineBuilder<TState> where TState : struct, Enum
{
    private static ReadOnlyDictionary<TState, IOperation<TState>>? _operations;
    private static StateContext<TState>? _buffer;

    // ReSharper disable once StaticMemberInGenericType
    private static string? _name;

    public static void SetStateContext(Action<StateContextBuilder> bufferBuilder)
    {
        if (_buffer is not null)
            throw new InvalidOperationException("Buffer is already initialized.");
        var builder = new StateContextBuilder();
        bufferBuilder(builder);
        _buffer = builder.Build();
    }

    public static void SetAssembly<T>(string name) where T : class
    {
        SetName(name);
        SetStateContext<T>();
        SetStateOperation<T>();
    }

    private static void SetStateContext<T>() where T : class
    {
        var machineType = typeof(T);
        var records = machineType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(v => v.FieldType.IsGenericType && v.FieldType.GetGenericTypeDefinition() == typeof(RecordKey<,>))
            .ToArray();
        var builder = new StateContextBuilder();
        var method = builder.GetType().GetMethod(nameof(StateContextBuilder.Add))!;

        foreach (var record in records)
        {
            var genericMethod = method.MakeGenericMethod(record.FieldType.GenericTypeArguments[1]);
            genericMethod.Invoke(builder, [record.GetValue(null)]);
        }

        _buffer = builder.Build();
    }

    private static void SetStateOperation<T>() where T : class
    {
        var machineType = typeof(T);
        var operationTypes = machineType.GetNestedTypes(BindingFlags.NonPublic)
            .Where(v => v is { IsClass: true, IsSealed: true } && typeof(IOperation<TState>).IsAssignableFrom(v))
            .ToArray();
        SetOperation(operationTypes);
    }

    public static void SetOperation(IOperation<TState>[] operations)
    {
        if (_operations is not null)
            throw new InvalidOperationException("Operations are already initialized.");
        ArgumentNullException.ThrowIfNull(operations);
        var operationStates = new HashSet<TState>(operations.Select(op => op.State));
        if (operationStates.Count != Enum.GetValues<TState>().Length)
        {
            throw new ArgumentException("Operations must match the states defined in the enum.");
        }

        _operations = new ReadOnlyDictionary<TState, IOperation<TState>>(operations.ToDictionary(v => v.State));
    }

    public static void SetOperation(Type[] operationTypes)
    {
        if (_operations is not null)
            throw new InvalidOperationException("Operations are already initialized.");
        ArgumentNullException.ThrowIfNull(operationTypes);
        var operations = new List<IOperation<TState>>();
        foreach (var type in operationTypes)
        {
            if (!typeof(IOperation<TState>).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    $"Type '{type.Name}' does not implement IOperation<{typeof(TState).Name}>.");
            }

            var operation = (IOperation<TState>)Activator.CreateInstance(type)!;
            operations.Add(operation);
        }

        _operations = new ReadOnlyDictionary<TState, IOperation<TState>>(operations.ToDictionary(v => v.State));
    }

    public static void SetName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_name is not null)
            throw new InvalidOperationException("Name is already initialized.");
        _name = name;
    }

    public static IStateMachine<TState> Create()
    {
        if (_buffer is null)
            throw new InvalidOperationException("Buffer is not initialized.");
        if (_operations is null)
            throw new InvalidOperationException("Operations must be initialized.");
        if (_name is null)
            throw new InvalidOperationException("Name must be set before creating a new machine.");
        return new StateMachine<TState>(_name, _buffer.Fork(true), _operations);
    }

    public class StateContextBuilder
    {
        private readonly StateContext<TState> stateContext;

        internal StateContextBuilder()
        {
            stateContext = new StateContext<TState>();
        }

        public StateContextBuilder Add<TValue>(RecordKey<TState, TValue> record) where TValue : notnull
        {
            stateContext.AddStateRecord(record);
            return this;
        }

        public StateContext<TState> Build()
        {
            return stateContext;
        }
    }
}