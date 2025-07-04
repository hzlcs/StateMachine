using System;
using Xunit;
using StateMachine;
using StateMachine.Interfaces;
using StateMachineTest.StateMachines;

namespace StateMachineTest;

public class StateMachineTests
{
    

    [Fact]
    public void DeviceStateMachine_Builder_NormalFlow_Success()
    {
        // 使用建造者模式创建状态机
        var stateMachine = DeviceStateMachine.CreateStateMachine("123", 123, "TestDevice");
        
        // 初始状态应为Init
        Assert.Equal(DeviceState.Init, stateMachine.CurrentState);

        // 跳转到自检
        stateMachine.MoveNext(out _);
        Assert.Equal(DeviceState.SelfCheck, stateMachine.CurrentState);
        // 跳转到加载原料
        stateMachine.MoveNext(out _);
        Assert.Equal(DeviceState.LoadMaterial, stateMachine.CurrentState);

        // 跳转到启动生产
        stateMachine.MoveNext(out _);
        Assert.Equal(DeviceState.StartProduction, stateMachine.CurrentState);

        // 跳转到完成
        stateMachine.MoveNext(out _);
        Assert.Equal(DeviceState.Finish, stateMachine.CurrentState);
    }
    
}