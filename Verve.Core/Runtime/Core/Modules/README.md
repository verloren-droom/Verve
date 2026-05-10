# 模块容器

## 概述

运行时模块容器用于：
- 管理模块安装、卸载、依赖校验与生命周期编排
- 驱动多个系统在不同 Tick 阶段执行（[`Early/Physics/Gameplay/Late`](./ITick.cs)）
- 通过模块清单批量创建模块


## 快速开始

### 1. 声明模块类

```csharp
using Verve;
using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;


[Serializable]
public sealed class FooModule : GameModule
{
    protected override ValueTask OnInstall(GameModuleContext context)
    {
        context.AddTickSystem(new FooTick());
        return default;
    }

    public void Print()
    {
        Debug.Log("FooModule");
    }

    private sealed class FooTick : IGameplayTick
    {
        public void GameplayTick(float deltaTime)
        {
            Debug.Log($"Tick {deltaTime}");
        }
    }
}

[Serializable]
public sealed class BarModule : GameModule, IGameModuleDependencies
{
    IReadOnlyList<Type> IGameModuleDependencies.Dependencies => new[] { typeof(FooModule) };

    protected override ValueTask OnInstall(GameModuleContext context)
    {
        context.GetModule<FooModule>()?.Print();
        return default;
    }
}
```

### 2. 手动安装模块

```csharp
using Verve;
using UnityEngine;


public sealed class Test1 : MonoBehaviour
{
    private GameModulesHandle m_Handle;

    private void Awake()
    {
        m_Handle = Game.CreateModules();

        var modules = m_Handle.Modules;
        modules.Install<FooModule>();
        modules.GetModule<FooModule>()?.Print();
    }

    private void OnDestroy()
    {
        m_Handle?.Dispose();
        m_Handle = null;
    }
}
```

### 3. 使用运行时模块清单

```csharp
using Verve;
using UnityEngine;


public sealed class Test2 : MonoBehaviour
{
    private GameModulesHandle m_Handle;

    private void Awake()
    {
        var manifest = new GameModuleManifest();

        manifest.Add<FooModule>();
        manifest.Add<BarModule>();

        m_Handle = Game.CreateModules(manifest);
    }

    private void OnDestroy()
    {
        m_Handle?.Dispose();
        m_Handle = null;
    }
}
```


### 4. 使用模块清单资源

```csharp
using Verve;
using UnityEngine;


public sealed class Test3 : MonoBehaviour
{
    [SerializeField] private GameModuleManifestAsset m_ManifestAsset;

    private GameModulesHandle m_Handle;

    private void Awake()
    {
        m_Handle = Game.CreateModules(m_ManifestAsset?.ToManifest());
    }

    private void OnDestroy()
    {
        m_Handle?.Dispose();
        m_Handle = null;
    }
}
```

## 核心 `API`

- `Game.CreateModules(...)`：创建一个 `GameModulesHandle`，由调用方显式持有和释放模块容器
- `GameModuleManifest.Add<T>()` / `Add(...)`：向运行时清单添加模块类型；默认可直接使用无参构造函数，也可显式传入创建工厂
- `GameModuleManifestAsset.ToManifest()`：把清单资源转换为运行时清单
- `GameModulesHandle.Modules` / `TryGetModules(...)`：获取当前句柄持有的容器
- `GameModules.Install<T>()` / `InstallAsync<T>()`：使用无参构造函数创建并安装单个模块
- `GameModules.Install(Func<GameModule>)` / `InstallAsync(Func<GameModule>)`：使用模块创建工厂创建并安装单个模块
- `GameModules.InstallFromManifest(...)` / `InstallFromManifestAsync(...)`：按清单配置批量安装模块
- `GameModules.Uninstall<T>(...)` / `UninstallAsync<T>(...)`：卸载模块
- `GameModules.GetModule<T>()` / `TryGetModule<T>(...)`：按模块类型获取模块
- `GameModules.InstalledModules` / `CopyModulesTo(...)`：读取已安装模块
- `GameModuleContext.AddTickSystem(...)` / `RemoveTickSystem(...)`：在模块生命周期内注册或移除 `Tick` 对象