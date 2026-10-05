<div align="center" background-color="transparent" style="text-align:center; background-color:transparent;">
<pre style="background-color:transparent;">
██╗   ██╗███████╗██████╗ ██╗   ██╗███████╗
██║   ██║██╔════╝██╔══██╗██║   ██║██╔════╝
██║   ██║█████╗  ██████╔╝██║   ██║█████╗  
╚██╗ ██╔╝██╔══╝  ██╔══██╗╚██╗ ██╔╝██╔══╝  
 ╚████╔╝ ███████╗██║  ██║ ╚████╔╝ ███████╗
  ╚═══╝  ╚══════╝╚═╝  ╚═╝  ╚═══╝  ╚══════╝
</pre>
</div>

# Verve ![Experimental](https://img.shields.io/badge/status-experimental-orange.svg) [![License](https://img.shields.io/badge/license-MIT-green)](LICENSE.md)

> `Verve` 是模块化游戏框架。框架提供模块容器、生命周期和通用工具等功能。

## 快速开始

在 `Unity Package Manager` 中通过 `Git URL` 安装：

```text
https://github.com/verloren-droom/Verve.git?path=Verve.Core
```

## 文档

| 模块 | 用途 | 文档 |
| --- | --- | --- |
| Verve.Core | 模块容器、事件、流程和通用基础 | [Verve.Core](Verve.Core/README.md) |
| Core / Modules | 模块安装、依赖、Tick 和释放 | [Modules](Verve.Core/Runtime/Core/Modules/README.md) |
| Core / Event | 类型化同步发布订阅 | [Event](Verve.Core/Runtime/Core/Event/README.md) |
| Core / Flow | 可等待步骤和条件分支 | [Flow](Verve.Core/Runtime/Core/Flow/README.md) |
| Core / Debug | 运行时控制台和调试页 | [Debug](Verve.Core/Runtime/Core/Debug/README.md) |
| Core / Tools | 可替换的无状态工具契约 | [Tools](Verve.Core/Runtime/Core/Tools/README.md) |
| Loader | Addressables/AssetBundle 资源和场景加载 | [Loader](Verve.Core/Runtime/Loader/README.md) |
| Table | 生成类型化访问器的配置表 | [Table](Verve.Core/Runtime/Table/README.md) |
| UI | View、ViewPart、Widget 和页面生命周期 | [UI](Verve.Core/Runtime/UI/README.md) |
| Network | 网络模块和 WebGL WebSocket 适配 | [Network](Verve.Core/Runtime/Network/README.md) |
| ACC | Actor-Component-Capability 世界模型 | [ACC](Verve.Core/Runtime/ACC/README.md) |
