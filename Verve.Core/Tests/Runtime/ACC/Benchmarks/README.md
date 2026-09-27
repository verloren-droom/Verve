# ACC 与 Unity Entities 实测

测试由 `AccEntitiesBenchmarks` 在 Unity Editor PlayMode 中执行，比较同一移动数据的创建、销毁、查询和更新路径。ACC 使用 `ActorManagerOptions(count, false, 2)` 关闭存储访问锁；能力 Tick 始终在世界线程执行。Entities 使用 `EntityManager`、`IJobChunk` 和 Burst。每项包含 2 次预热、9 次采样、每次 32 次稳态更新，报告中位数，单位为毫秒；准备、清理和日志不计入计时。

环境：Apple M1 Pro，macOS 13.5.2，Unity 6000.3.7f1，Entities 1.4.8。测试在 Editor Mono 下运行，未代表 Player 构建结果；Entities 的 Burst 路径还包含原生内存和工作线程开销，因此不能用托管分配数单独推断总成本。

| 操作 | 对象数 | ACC 中位数 | Entities 中位数 |
| --- | ---: | ---: | ---: |
| 创建并写入数据 | 100,000 | 24.346 | 40.501（批量） |
| 销毁 | 100,000 | 70.270 | 1.337（批量） |
| 收集查询 | 100,000 | 0.942 | 0.130 |
| 逐对象组件更新 | 100,000 | 10.065 | 78.021（EntityManager） |
| Capability 更新 | 100,000 | 23.928 | — |
| Burst Run | 100,000 | — | 0.223 |
| Burst 并行并等待 | 100,000 | — | 0.077 |

ACC 的目标是让每个游戏对象集中保存行为和决策，方便玩法开发、明确所有权和稳定顺序；Entities/Burst 更适合大规模同构数据的批量计算。两者的定位不同，实际项目应按数据规模和逻辑耦合方式选择，并以目标平台 Player 构建的独立基准复核。
