using System.Diagnostics;
using StateMachine;
using StateMachine.Interfaces;

namespace StateMachineTest.StateMachines;

public class DeviceStateMachine
{
    private static readonly RecordKey<DeviceState, string> Ip = new ("Ip");
    private static readonly RecordKey<DeviceState, int> Port = new("Port");
    private static readonly RecordKey<DeviceState, string> DeviceName = new("DeviceName");
    
    private static readonly RecordKey<DeviceState, string> Socket = new("Socket", DeviceState.Init);
    private static readonly RecordKey<DeviceState, bool> SelfCheckResult = new("SelfCheckResult", DeviceState.SelfCheck);
    private static readonly RecordKey<DeviceState, string> MaterialBatch = new("MaterialBatch", DeviceState.LoadMaterial);
    private static readonly RecordKey<DeviceState, string> ProductionBatch = new("ProductionBatch", DeviceState.StartProduction);
    private static readonly RecordKey<DeviceState, bool> ProductionResult = new("ProductionResult", DeviceState.Finish);

    static DeviceStateMachine()
    {
        StateMachineBuilder<DeviceState>.SetAssembly<DeviceStateMachine>("Device");
    }

    public static IStateMachine<DeviceState> CreateStateMachine(string ip, int port, string deviceName)
    {
        var stateMachine = StateMachineBuilder.Create<DeviceState>();
        stateMachine.InitState(Ip, ip);
        stateMachine.InitState(Port, port);
        stateMachine.InitState(DeviceName, deviceName);
        stateMachine.Initialize(out _);
        return stateMachine;
    }

    private sealed class DeviceInitOperation : Operation<DeviceState>
    {
        private string ip = null!;
        private int port;
        public override DeviceState State => DeviceState.Init;

        public override ResultCode Run(IStateContext<DeviceState> stateContext, out string errorMessage)
        {
            Debug.Assert(ip != null);
            stateContext.SetValue(Socket, ip + ":" + port);
            errorMessage = string.Empty;
            return ResultCode.Success;
        }

        public override bool CheckInitialized(IStateContext<DeviceState> stateContext, out string[] missingKeys)
        {
            List<string> missing = [];
            if (!stateContext.TryGetValue(Ip, out ip))
                missing.Add(Ip.Key);
            if (!stateContext.TryGetValue(Port, out port))
                missing.Add(Port.Key);
            if (missing.Count > 0)
            {
                missingKeys = [.. missing];
                return false;
            }
            missingKeys = [];
            return true;
        }
    }

    private sealed class DeviceSelfCheckOperation : Operation<DeviceState>
    {
        public override DeviceState State => DeviceState.SelfCheck;

        public override ResultCode Run(IStateContext<DeviceState> stateContext, out string errorMessage)
        {
            stateContext.SetValue(SelfCheckResult, true);
            errorMessage = string.Empty;
            return ResultCode.Success;
        }
    }

    private sealed class DeviceLoadMaterialOperation : Operation<DeviceState>
    {
        public override DeviceState State => DeviceState.LoadMaterial;

        public override ResultCode Run(IStateContext<DeviceState> stateContext, out string errorMessage)
        {
            stateContext.SetValue(MaterialBatch, "<UNK>");
            // 假设加载原料成功
            errorMessage = string.Empty;
            return ResultCode.Success;
        }
    }

    private sealed class DeviceStartProductionOperation : Operation<DeviceState>
    {
        public override DeviceState State => DeviceState.StartProduction;
        
        private IStateRecord<string>? productionBatch;

        public override ResultCode Run(IStateContext<DeviceState> stateContext, out string errorMessage)
        {
            Debug.Assert(productionBatch != null);
            stateContext.SetValue(ProductionBatch, "<UNK>");
            // 假设生产成功
            errorMessage = string.Empty;
            return ResultCode.Success;
        }

        public override bool CheckInitialized(IStateContext<DeviceState> stateContext, out string[] missingKeys)
        {
            productionBatch = stateContext.GetStateRecord(ProductionBatch);
            return base.CheckInitialized(stateContext, out missingKeys);
        }
    }

    private sealed class DeviceFinishOperation : Operation<DeviceState>
    {
        public override DeviceState State => DeviceState.Finish;

        public override ResultCode Run(IStateContext<DeviceState> stateContext, out string errorMessage)
        {
            stateContext.SetValue(ProductionResult, true);
            errorMessage = string.Empty;
            return ResultCode.Success;
        }
    }
}

public enum DeviceState
{
    Init,
    SelfCheck,
    LoadMaterial,
    StartProduction,
    Finish
}