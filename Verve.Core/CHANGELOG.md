# Changelog

## Unreleased

- 移除内置 I18N 模块及翻译资源、文本组件、生成引用、编辑器和专属测试；清理项目注册与生成文件，无兼容层。UI 回收压力与组件停用时机改为独立的生命周期测试。

- 编辑器按模块拆分程序集，公共编辑器仅引用 Core，Addressables 编辑器依赖收敛到 Loader；模块内部访问授权同步限定到对应编辑器。Loader 内容构建移除 UI 校验耦合，项目通过公开的 `UIBuildValidator.Validate()` 组合构建流程。
- 修复通用头部绘制刷新、提前提交借用序列化对象的问题；保留宿主编辑器的修改和撤销事务。保存工具设置改为只保存目标资源，移除全项目保存与刷新。
- 模块清单增加 `GameModuleEditor` 扩展点，通过属性声明自动选择编辑器，默认绘制直接复用；清单负责编辑器所有权、Undo/Redo、Apply/Revert 与资源引用保存。修复隐藏导入器自动应用以及保存后多余重载造成的编辑器重建。
- 编辑器目录对应运行时职责；通用 `CoreEditorUtility` 迁入 `Editor/Core/Utilities`，工具查询归到 `GameToolEditorUtility`。无 Unity 依赖的 CSV 逻辑直接迁到 `Game.CsvUtility`。
- 页面释放与创建失败时统一停用节点后再销毁，组件即时收到停用消息。

- Table 的 CSV 解析与写入提取到 `Game.CsvUtility`，合并字段收尾流程。

- 列表公共搜索栏增加 `#` 长度和 `Aa` 大小写按钮，Table／UI 共用；配置表编辑器复用同一搜索栏，删除专用 GUI 实现。精确匹配分别比较名称、路径，避免标签装饰和拼接文本妨碍命中。
- UI 区分无参数 `ViewBase` 与有参数 `ViewBase<TArgs>`，统一使用 `Open<TView>(args)`；参数派生自 `ViewArgs`，加载前校验，类型信息缓存。关闭、失败和释放时清除参数引用，业务数据保持借用；不增加模块打开接口。
- 已打开页面和合并中的异步请求拒绝不同参数；控制器与打开后回调从具体页面读取强类型数据。`OpenNew` / `OpenNewAsync` 收敛为 `openMode: UIViewOpenMode.New`，迁移项目示例并删除旧 `object data` 入口。

- 整理运行时与编辑器代码，保持 Unity 2021.3 LTS 为最低版本，删除低于最低版本的循环和编辑器分支；保留通用扩展能力及独立对象池，不修改 ACC。
- 修复对象池回调重入、协程执行器销毁、事件订阅通知失败和释放后引用滞留的所有权问题；异常继续向调用方报告。
- AssetBundle 构建生成 `verve-bundles.json`，直接替代运行时打开所有资源包建立索引的流程；按文件或 URL 异步加载，支持 Android APK 和 WebGL 路径。
- Network 增加仅用于 WebGL 的浏览器 WebSocket 适配，由模块释放连接和有界接收队列；同步 Addressables 网络加载在 WebGL 明确要求改用异步入口。
- 合并属性绘制与组件验证，清理单选弹窗冗余捕获、页签重复测量、序列化旧 BOM 分支和压缩空值兜底；预览场景、临时序列化对象与导入失败产生的资源自动释放。

- 单选弹窗在执行回调前关闭，避免确认对话框结束后又关闭选择弹窗而改变焦点；移除工具设置中的强制聚焦与延迟回调。

- 工具设置切换不同实现前增加确认，展示当前项与目标项；取消保留配置，重复选择不写入，默认与显式选择指向同一实现时不提示替换。

- 移除 JSON、日志、AES、GZip 工具的 `InstanceBase<T>` 继承，由工具注册表统一持有实例；Table 固定 JSON 读写直接使用 Unity 的 `JsonUtility`，删除工具单例入口的调用。

- 工具声明迁为 `GameToolAttribute(displayName, defaultImplementationType)`：展示名与可选默认实现统一声明，设置页和搜索采用展示名，持久化仍按接口类型识别；移除旧特性，保留原 GUID。

- 增加 `CoreEditorUtility.ShowListPopup`，统一 Table／UI 列表的搜索、滚动、图标、提示与选择关闭；弹窗实现保持内部，显示数据按打开时缓存，关闭清除借用引用。
- 列表搜索仅在条件变化时重新计算，只绘制可见行；Table 文件信息不再逐帧读取，移除解析失败后继续展示空表的兜底。

- 加解密统一迁回 Tool：`ICrypto` 每次显式接收借用密钥，默认使用内部无状态 `AesCrypto`；移除 `Game.CryptoUtility`，文本／流扩展同步迁移，不保留旧重载。
- 增加 `Game.HashUtility`，通过 `HashAlgorithmName` 计算 MD5、SHA 系列文件／流指纹，提供同步与可取消异步调用；文件资源自动释放，借用流保持打开。

- 增加 `ScriptableObjectInstanceBase<T>`：按类型约定资源路径，懒加载唯一持久化资源，缺失、重复或类型错误明确报告；`GameToolSettings` 复用，删除专用加载实现。
- 模块项目设置统一到 Project Settings → Verve → Modules；`CoreEditorUtility.ModuleSettingsRoot` 提供公共根路径，各模块直接注册 Unity 的 `SettingsProvider`。
- 配置表设置迁到 Project Settings → Verve → Modules → Table，删除独立窗口与旧设置路径；恢复默认改为草稿，应用时统一迁移输出与保存。
- 工具设置实现右侧增加脚本选择定位，复用 `CoreEditorUtility.FindMonoScriptForType`，仅点击时查找，运行模式仍可定位。

- 工具统一为 `IGameTool`、可选 `GameToolAttribute` 与 `GameToolConfiguration`；接口自动发现，默认实现与项目实现均按需创建。删除工具作用域、初始化／卸载及运行模式重置。
- 固定 `Module` / `Tool` / `Utility` 边界：工具拒绝模块、Unity 对象及可释放类型，配置在脚本域内不可替换，失败不回退。项目工具需支持并发调用和操作内资源清理。
- Project Settings → Verve → Tools 在编辑与运行模式使用同一配置，提供应用配置并重载脚本按钮。关闭 Domain Reload 时工具实例跨运行模式保持一致；配置加载、设置页与构建共同拒绝重复或错放设置文件。
- AES 实现位于 `Tools/Crypto`，借入密钥并在单次操作内释放加密器。移除旧入口，无兼容转发。
- 模块异步收尾迁回 `Modules`，`Game.ShutdownModulesAsync` 不影响工具可用性。日志输出开关移出工具接口，后台日志不再读取 Unity 主线程帧号。
- 构建时校验工具实现并生成裁剪配置，工具类型查询位于 `GameToolEditorUtility`，通用 link.xml 写入位于 `CoreEditorUtility`。配置表固定使用内部 JSON 编码器。

- 共用编辑器工具集中到 `CoreEditorUtility`：按钮与列表行绘制、类型对应脚本查询、源 Prefab 查询、菜单节点解析、文本资源原子写入与导入。删除 `ButtonEditorHelper`、`EditorListStyle` 和模块内重复实现。

- HTTP 工具统一为 `Game.HttpUtility`，文件更名为 `Game.HttpUtility.cs` / `Game.HttpUtility.Net.cs`，同步迁移调用方并保留原 GUID。
- 补齐 `Game.ReflectionUtility` 的实例字段、属性读写和逐级成员查找；复用到模块清单与按钮参数解析，成员缺失、类型不符和访问器异常直接报告。
- 按钮参数仅在点击后读取，避免 Inspector 重绘反复执行 getter 或参数提供方法；无参方法按完整签名查找，消除同名重载歧义。
- UI、Loader、Table 调用方直接复用反射、路径和异常工具；删除重复字段读写及路径 helper。UI 代码生成、模块清单和 link.xml 写入统一使用 Core 原子文件工具。
- link.xml 保留图使用统一字段枚举；移除版本标签每次绘制创建的无用透明纹理，消除未释放的纹理分配。

- HTTP 工具迁入 `Verve.Core`，合并 `UnityWebRequestUtility` 到 `HttpUtility`；配置表复用统一 GET 入口，删除重复文本读取方法和旧工具类型。
- 保留 Unity / 非 Unity 适配，上传请求的创建与发送在同一释放范围内完成；普通请求自动释放，`SendAsync` 仅借用外部请求。

- 移除 UI 导航可视化工具及 View、Widget Inspector 中的导航筛选和可视化开关，删除场景绘制回调与偏好设置读写。

- 精简 `UIModule`：移除 Prefab 热重载监听及恢复流程；复用已有页面时配置必须相同，冲突时显式释放后重建，或通过 `UIViewOpenMode.New` 创建独立实例。
- 合并异步打开登记，加载前接管请求，结束跟踪后交付结果；统一页面配置，移除重复关闭处理和创建计数。
- 修复加载器重入时重复创建、嵌套创建失败时根节点提前解锁、失效页面清理时误删新实例登记，以及单独移除绑定组件后遗留页面节点的问题。

- 修复 UI、Loader 和配置表在取消回调抛错时跳过等待或清理的问题；取消前记录正在执行的任务，清理完成后汇总原始错误与释放错误。
- 修复重复资源加载在释放临时引用失败时遗留缓存引用的问题；尚未交出的句柄由框架自动回收。
- 合并 Loader 安装失败与卸载的清理流程，资源引用批量释放复用 Core 内部工具；不增加公开扩展或兼容入口。

- 统一核心与各模块的简短 XML 注释；继承或实现接口的成员使用 `inheritdoc`，新增方法补齐参数说明。
- `EventDispatcher`、事件订阅句柄、`AssetLoadScope` 和默认模块工厂复用 `DisposableObject`；批量资源清理统一到 Core 内部的 `ResourceUtility`，先移除登记、再释放，最后汇总错误。
- 简化单表达式成员和模块查询；`PropertyProxy` 使用标准事件与默认相等比较器，删除 `AddListener` / `RemoveListener` 包装。
- 日志直接使用 `string.Format`，格式错误直接抛出；移除配置表旧聚合入口的清理分支。

- 以容器拥有的 `IGameModuleObserver` / `GameModuleOperationResult` 替代 `OnModulesChanged`，报告模块操作完成结果与耗时；删除旧事件与合并通知状态，观察者错误在操作完成必要清理后报告。
- `IGameModuleFactory.Configure` 在接管和依赖预留后执行，先于清单条目配置与安装；配置失败自动释放。观察者在模块、调度器和工厂之后释放。

- 增加容器级 `GameModulesOptions`，统一默认/自定义模块工厂和 Tick 调度器；扩展实例由容器独占并自动释放。
- 通过 `IGameModuleFactory` 和已有调度接口定制；默认实现保持内部，代码、资源清单和程序集安装统一创建策略，扩展仅在容器创建时确定。
- Unity 适配组件更名为 `GameModulesLifetime`，仅触发句柄释放，模块和扩展所有权保持在框架容器内。
- 模块配置在框架接管后执行；拒绝共享扩展、借用模块与错误工厂结果，自定义调度器同样受容器 Tick 重入保护。

- 删除运行时模块热替换、失败后重装与 Tick 恢复；卸载统一终止并释放资源，异常汇总后报告。
- 模块实例保存唯一容器归属，移除全局所有权表；清单按需逐项创建，取消后不创建剩余实例。
- 新增 `Game.CreateModules(Component/GameObject)` 自动宿主绑定，销毁宿主时异步取消、卸载和释放容器。
- 资源作用域真正接管句柄释放权；修复绑定宿主在异步加载期间销毁时的句柄泄漏，清除释放后句柄的强引用。
- Tween 收敛为单一 `Tween.Run` 进度协程，删除 Builder、Runner 和属性动画包装。

- 将路径、任务等待、异常汇总、引用比较、原子文件写入和 UnityWebRequest 适配统一到 Core。
- 移除旧框架接口兼容与静默业务兜底，迁移配置表异步安装、网络命名空间、加密接口。
- 修复事件键碰撞、对象池失败清理、网络取消与下载覆盖、UI 清理错误传播。
- 保留自带对象池与引擎/平台适配，验证 Unity 2021.3、2022.3、Unity 6 和非 Unity 分支。
- 替换 Plastic 私有 JSON 依赖，补齐显式包依赖；ACC 未修改。

详细契约与调用迁移见 [MIGRATION.md](MIGRATION.md)。
