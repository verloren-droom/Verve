# `ACC` 架构

## 概述

`ACC（Actor-Component-Capability）`架构，是一种实体组件系统设计模式，旨在通过数据与逻辑分离，适用于技能系统、战斗系统、`AI`行为等场景。

## 核心概念

| 组件             | 职责    | 说明                        |
|----------------|-------|---------------------------|
| **Actor**      | 实体标识符 | 64位整数结构体，高32位索引+低32位版本号   |
| **Component**  | 数据容器  | 结构体实现`IComponent`接口，纯数据存储 |
| **Capability** | 能力逻辑  | 继承`Capability`基类，执行游戏行为   |
| **Sheet**      | 表单    | 批量应用组件和能力的配置              |
| **World**      | 世界管理器 | 实体、组件、能力与表单的统一入口          |

## 快速开始

### 1. 创建Component

```csharp
using Verve;
using System;

/// <summary>
///   <para>位置组件</para>
/// </summary>
[Serializable]
public struct PositionComponent : IComponent
{
    public float x;
    public float y;
    public float z;
}

/// <summary>
///   <para>速度组件</para>
/// </summary>
[Serializable]
public struct VelocityComponent : IComponent
{
    public PositionComponent linear;
    public float maxSpeed;
}
```

### 2. 创建Capability

```csharp
using Verve;
using System;

/// <summary>
///   <para>移动能力</para>
/// </summary>
[Serializable]
public class MovementCapability : Capability
{
    /// <summary>
    ///   <para>设置（仅在添加后执行一次）</para>
    /// </summary>
    protected override void OnSetup()
    {
        // 依赖组件
        Require<PositionComponent>();
        Require<VelocityComponent>();
        
        // 设置执行顺序
        SetTick(TickGroup.Gameplay, 0);
    }

    /// <summary>
    ///   <para>当被激活时执行每帧调用</para>
    /// </summary>
    protected override void TickActive(in float deltaTime)
    {
        ref var pos = ref this.GetComponent<PositionComponent>();
        ref var velocity = ref this.GetComponent<VelocityComponent>();

        pos.x += velocity.linear.x * deltaTime;
        pos.y += velocity.linear.y * deltaTime;
        pos.z += velocity.linear.z * deltaTime;
    }
}
```

### 3. 使用

```csharp
using Verve;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float m_MoveSpeed = 15f;
    private string m_WorldName = "TestWorld";
    private Actor m_Actor;

    private void Start()
    {
        // 创建世界
        Game.CreateWorld(m_WorldName);
        Game.GotoWorld(m_WorldName);

        // 创建Actor
        m_Actor = Game.World.CreateActor();

        // 添加组件并修改组件值
        ref var pos = ref Game.World.AddComponent<PositionComponent>(m_Actor);
        pos.x = transform.position.x;
        pos.y = transform.position.y;
        pos.z = transform.position.z;

        ref var velocity = ref Game.World.AddComponent<VelocityComponent>(m_Actor);
        velocity.maxSpeed = m_MoveSpeed;

        // 添加能力
        Game.World.AddCapability<MovementCapability>(m_Actor);
    }
}
```

**核心API**：
- `Game.CreateWorld(string name)`：创建世界
- `Game.GotoWorld(string name)`：跳转世界
- `Game.DestroyWorld(string name)`：销毁世界
- `Game.World`：获取当前活跃世界
- `World.CreateActor()`：创建Actor
- `World.DestroyActor(Actor)`：销毁Actor
- `World.AddComponent<T>(Actor, NetworkSyncDirection)`：添加组件
- `World.GetComponent<T>(Actor)`：获取组件
- `World.HasComponent<T>(Actor)`：判断组件是否存在
- `World.TryGetComponent<T>(Actor, out T)`：尝试获取组件
- `World.RemoveComponent<T>(Actor)`：移除组件
- `World.SetComponent<T>(Actor, in T)`：设置组件
- `World.AddCapability<T>(Actor)`：添加能力
- `World.RemoveCapability<T>(Actor)`：移除能力
- `World.ApplySheet(Actor, CapabilitySheet)`：应用表单
- `World.RemoveSheet(Actor, SheetInstance)`：移除表单