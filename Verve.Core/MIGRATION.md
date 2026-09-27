# 框架整理与接口迁移

本次直接迁移框架接口，不保留旧名称、旧命名空间、旧数据格式读取或异常后的默认业务结果。ACC 的源码、元数据和测试不在修改范围内。

## 本轮运行时与编辑器整理

- 直接移除内置 I18N 模块、翻译表、文本组件、生成引用及全部专属编辑器入口，不提供兼容接口。清理模块清单中的注册项与项目生成文件、翻译资源；通用 CSV、模块和 UI 生命周期能力继续保留。
- `Verve.Editor` 仅引用 `Verve.Core`；UI、Table、Loader 的编辑器代码分别归入 `Verve.UI.Editor`、`Verve.Table.Editor`、`Verve.Loader.Editor`。自定义编辑器程序集按实际使用的类型更新 asmdef 引用，不提供旧程序集转发。
- Loader 内容构建不再直接校验 UI。Player 构建继续由 UI 自己的构建回调校验；仅构建内容包时，项目入口先调用 `Verve.Editor.UIBuildValidator.Validate()`，再调用 `LoaderBuildPipeline.BuildHotUpdateContent(target)`。未使用 UI 的项目无需这一步。
- `CoreEditorUtility.DrawHeaderToggle` 只修改传入属性，调用方统一负责 `SerializedObject.Update` 与 `ApplyModifiedProperties`。`MarkDirtyAndSave` 仅保存目标资源所属文件，不再提交整个项目的脏资源或刷新资源库。
- `GameModuleDependency` 仅用于必需模块；可选组合由项目入口完成。`TryGetDependency` 仍要求依赖已声明，不是可选模块查找入口。
- 编辑器目录按运行时职责对应：`Core/Modules`、`Core/Tools`、`Core/Utilities`。`CoreEditorUtility` 及其通用搜索、列表部分迁入 `Editor/Core/Utilities`，仅保留通用编辑器功能。
- `CoreEditorUtility.ParseCsv` / `AppendCsvRow` 直接迁为运行时的 `Game.CsvUtility.Parse` / `AppendRow`。工具接口及实现查询改用 `GameToolEditorUtility.FindToolContracts` / `FindRuntimeImplementations`，无旧入口转发。
- 模块字段自定义绘制使用 `GameModuleEditor` 与 `[CustomGameModuleEditor(typeof(MyModule))]`，重写 `OnInspectorGUI(SerializedProperty module)`；可调用基类绘制默认字段。清单统一拥有编辑器实例、保存和撤销；参数仅本次绘制借用，编辑器不拥有模块及其资源。默认界面无需注册，错误注册直接报告。

- 对象池继续提供 Unity 2021 可用的独立实现。创建、借出或归还回调改变池的释放状态／容量时，重新确认所有权；无法接管的对象通过销毁回调释放。
- 事件发布保留订阅快照语义。取消单项、清空事件或释放分发器后，旧句柄不再持有回调目标、事件键及分发器；记录通知失败时撤回尚未交给调用方的订阅。
- 协程执行器拥有运行中的操作。停止操作显式释放迭代器；执行器销毁取消等待 Unity 指令的任务，清理异常不妨碍状态与取消源释放。
- 压缩扩展不把 null 或空输入改成默认结果。空文本交给所选压缩实现，null 抛错。删除重复的 `Span<byte>` 重载，调用方使用其到 `ReadOnlySpan<byte>` 的隐式转换。
- 序列化文本只处理指定编码的前导码，不保留旧实现的 UTF-8 BOM 特判。借入流仍由调用方管理。
- 编辑器预览拥有预览场景，重新初始化、失败和关闭时清理；Prefab 资源始终借用。临时 `SerializedObject` 按作用域释放，属性绘制不覆盖外层 GUI 状态。
- 保留未被框架内部使用、但职责明确的通用控件。清理单选窗口内无效结果捕获，并统一关闭后回调；页签只测量一次宽度，空页签输入在进入布局前报错。
- 调试命令和窗口发现不再忽略程序集扫描／构造错误；空命令名、重复命令名直接报错。继承的方法只按声明扫描一次。

### AssetBundle 内容迁移

重新通过 `LoaderBuildPipeline.BuildHotUpdateContent` 或正常 Player 构建生成内容。输出目录包含 Unity 清单、资源包及 `verve-bundles.json`，部署时必须整体发布。目录记录 `AssetDatabase.GetAssetPathsFromAssetBundle` 返回的可寻址项目资源路径，框架使用固定 JSON 格式，不受项目 `ISerializer` 选择影响；未分配 Bundle 的隐式依赖不能作为独立加载地址。

安装时只读取清单及路径目录，实际资源包按需加载。目录缺失、重复路径或与清单不一致时安装失败；不回退到旧的运行时扫描。Android APK、远程地址和 WebGL 使用 `UnityWebRequestAssetBundle`，本地文件使用引擎文件接口。取消时释放已经产生的原生资源，卸载继续由框架的引用计数负责。

### 平台适配

`NetworkModule` 在 PC／移动端使用 `ClientWebSocket`；仅 `UNITY_WEBGL && !UNITY_EDITOR` 编译浏览器实现与 `.jslib`。浏览器连接、消息队列和事件处理函数随模块释放，不引入新的全局 Tool 或业务生命周期。保留 UTF-8、文本内 NUL、空消息和分段读取；二进制消息、连接错误、超限和取消显式报告。

浏览器接收队列的累计字节上限同步 `MaxReceiveMessageBytes`，并限制最多 1024 条待处理消息，避免空消息无限堆积。连接创建后调整字节上限同样生效，超限关闭连接并报告错误。

WebGL 的 Addressables 同步网络加载明确要求使用 `LoadAssetAsync`；URL 资源包尚未加载时同样走异步入口，不阻塞浏览器线程。HTTP 使用 Unity 原生请求，不绕过浏览器的 CORS、HTTPS／WSS 或文件系统约束。上述适配不代表已验证所有设备和浏览器，实际验证范围见 `VALIDATION.md`。

## Module、Tool 与 Utility

| 概念 | 职责 | 实例与资源 |
| --- | --- | --- |
| `Module` | 业务状态、依赖、持续执行和需要管理的资源 | 容器创建、安装、卸载并释放 |
| `Tool` | 可替换的全局无状态操作 | 框架按接口缓存实例，无安装、卸载或运行模式切换 |
| `Utility` | 固定实现的通用操作 | 直接调用，临时资源在单次调用内释放 |

`Tools` / `Utilities` 目录区分是否需要项目替换实现；类型仍位于 `Verve`。业务实体、View、Widget、普通资源对象和对象池等基础构件不因此强制归入这三类。脚本与目录移动保留 GUID，共用编辑器操作继续集中在 `CoreEditorUtility`。

### 工具扩展与约束

能力接口继承 `IGameTool`，项目新增接口与实现自动出现在 **Project Settings → Verve → Tools**。不需要向框架添加配置字段、工厂分支或编辑器列表。

```csharp
[GameTool("项目编码", typeof(ProjectCodec))]
public interface IProjectCodec : IGameTool
{
    string Encode(string value);
}

public sealed class ProjectCodec : IProjectCodec
{
    public string Encode(string value) => value.ToUpperInvariant();
}

var text = Game.GetTool<IProjectCodec>().Encode("verve");
```

`GameToolAttribute` 可选，声明展示名与可选的默认实现，例如 `[GameTool("项目编码", typeof(ProjectCodec))]` 或仅声明名称 `[GameTool("内容签名")]`。不标记时按接口名称展示，仍自动发现；标记时展示名不能为空。声明不继承给派生接口。项目选择优先；类型失效、构造失败、默认实现非法均报错，不切换到其他实现。没有默认实现又没有项目选择的接口在调用时报错。

展示名只用于设置页及其搜索；提示保留完整接口名和程序集，配置标识仍为接口类型的程序集限定名，同名工具不会共用配置。默认实现属性为 `DefaultImplementationType`。直接移除 `GameToolDefaultAttribute` 和旧构造入口，更名保留脚本 GUID。

- 工具实现必须为公开、非抽象、无开放泛型的普通类，具有公开无参构造函数；默认实现允许为接口程序集中的内部类。
- 接口和实现均不得混入 `IGameModule`、`IDisposable` 或 `IAsyncDisposable`；Unity 对象也不能注册为工具。需要长期持有文件、连接、密钥、订阅、后台任务或可变业务状态时使用模块。
- 工具操作需可并发调用，不依赖编辑／运行模式；构造函数不得获取其他工具。操作内允许复用其他工具，不建立构造依赖图。
- 临时流、加密器等由实现用 `using` 在操作内清理；借入的对象保留调用方所有权。框架不会靠反射扫描字段来推测纯度，项目实现仍需遵守无状态约定。
- 配置只保存类型映射。项目实现与默认实现均在首次访问时创建，同一接口在脚本域内只创建一次，构造异常同样缓存。不同接口不隐式共享实例。

当前工具包括序列化、压缩、日志与加解密。留空时序列化使用 JSON、压缩使用 GZip、日志使用引擎／控制台日志，加解密使用内部 AES 实现。加解密调用必须显式传入密钥，框架不生成默认密钥。

### 配置生效与编辑器可用性

首次自定义选择创建 `Assets/Resources/Verve/GameToolSettings.asset`。编辑态和运行态读取同一配置；配置在脚本域内固定。修改后点击 **应用配置并重载脚本**，或等待下一次正常脚本重载。关闭 Domain Reload 时，仅进入／退出运行模式不会替换工具实例。运行中禁止编辑选择。

设置页选择不同实现时先确认替换，弹窗展示工具名称、当前项与目标项，并说明脚本重载后生效。取消不写入配置；重复选择当前项直接返回。默认项与显式选择指向同一类型时仅更新选择方式，不弹出实现替换确认。

单选先关闭选项弹窗，再执行选择回调与替换确认；焦点交回 Unity 处理，不在确认结束后调用 `Focus` 或登记延迟聚焦。多选继续保持弹窗打开。

`GameToolSettings` 仅允许在上述固定路径保存一份。配置加载、设置页与构建处理共用唯一性校验；重复、错放或路径被其他资源占用时明确报错，列出冲突路径。设置页监测项目资源变化，冲突期间禁用编辑；框架不自动删除、迁移或合并文件。没有配置文件才使用各接口的默认声明。

Unity 自动在主线程预读配置，不构造工具；调用工具无需先创建或安装任何模块。首次读取资源配置需在主线程，预读之后可在后台调用符合并发约定的工具。模块创建或卸载过程也可以使用工具；模块会话结束不会清理或禁用工具。

非 Unity 宿主可以在首次访问前指定实现，无需初始化或退出工具：

```csharp
Game.ConfigureTools(new GameToolConfiguration()
    .Set<ISerializer, ProjectSerializer>()
    .Set<ICompression, ProjectCompression>());
```

`ConfigureTools` 立即保存配置快照，不执行构造函数，调用后禁止再次配置。移除 `InitializeTools`、`GameToolScope` 和工具结束状态，不保留兼容入口。原来混在工具内的模块收尾迁回 `Modules`，入口为 `Game.ShutdownModulesAsync()`；其等待 `Game.CreateModules` 创建的容器及已经开始的异步卸载，直接创建的 `GameModules` 仍由其调用方管理。

构建处理校验项目选择与默认实现是否适用于当前 Player，并生成 `Assets/Verve.Generated/Tools.link.xml`；不执行候选构造函数。配置表直接使用 Unity 的 `JsonUtility` 处理固定 JSON 格式，避免项目的序列化工具选择改变 `.ctable` 协议。

内置工具实现不继承 `InstanceBase<T>`，实例统一由 `GameToolRegistry` 按接口懒创建并缓存。删除工具实现上的 `Instance` 入口；业务调用通过 `Game.Serializer`、`Game.Compression`、`Game.Crypto` 或 `Game.GetTool<T>()` 获取。`GameToolSettings` 是配置资源，继续使用 `ScriptableObjectInstanceBase<T>` 管理唯一性，不属于工具实现。

`InstanceBase<T>` 当前仅由 ACC 的两个现有实现继承；ACC 不在修改范围，因此保留原基类供其编译使用，不新增兼容包装。Table 的协议明确要求 JSON，不经过可替换的 `Game.Serializer`；项目切换序列化工具不会改变表文件格式。

### 加解密与日志迁移

加解密统一归入 `Tools/Crypto`：`ICrypto.Encrypt(data, key)` / `Decrypt(encrypted, key)` 每次借入密钥，内部 `AesCrypto` 为默认实现，项目可以在工具设置中选择自己的 `ICrypto`。移除 `Game.CryptoUtility`，不保留转发接口。AES 算法保持 AES-256-CBC + HMAC-SHA256，要求 64 字节密钥；加密器与 HMAC 当次释放，临时密钥副本清零。实现不保存、修改或释放借入密钥；密钥的获取与长期保存由所属业务管理。

文本、Base64 和流扩展同步增加 `key` 参数。例如 `Game.Crypto.Encrypt(text, key)` 返回 Base64，`Game.Crypto.Decrypt(base64, key)` 返回文本。默认算法不等于默认密钥，无参数密钥入口已移除。

`ILogger.IsEnabled` 从工具契约移除，`Game.EnableLog` 控制框架日志入口的输出，日志实现保持无状态。

## 文件与流的内容指纹

`Game.HashUtility` 属于固定标准算法的 Utility，不注册为全局 Tool。调用显式传入 .NET `HashAlgorithmName`，支持运行时提供的 MD5、SHA-1、SHA-256、SHA-384、SHA-512；未知或平台不支持的算法直接报错，不替换为其他算法。

```csharp
using System.Security.Cryptography;

var sha256 = Game.HashUtility.ComputeFileHash(path, HashAlgorithmName.SHA256);
var md5 = await Game.HashUtility.ComputeFileHashAsync(path, HashAlgorithmName.MD5, cancellationToken);
```

结果统一为小写十六进制字符串。文件入口自动关闭文件流；`ComputeHash(stream, algorithm)` / `ComputeHashAsync` 借用流，从当前位置读到末尾，不关闭或重置位置。按块读取，内存不随文件大小增长；取消和读取错误直接传播。流式实现复用 .NET `IncrementalHash`，不自行实现哈希算法。

默认建议 SHA-256；MD5 / SHA-1 可用于既有指纹格式，不能作为防恶意篡改的安全保证。普通摘要不包含身份认证，需要可信签名的流程由签名或加密能力处理。

## 全局资源与项目设置

新增 `ScriptableObjectInstanceBase<T>`，用于需要随 Player 发布的项目全局资源，不是 `Tool` 或 `Module` 的替代生命周期。派生类使用自身作为类型参数，建议声明为 `sealed`：

```csharp
namespace MyGame
{
    [UnityEngine.CreateAssetMenu(menuName = "MyGame/Project Settings")]
    public sealed class ProjectSettings : Verve.ScriptableObjectInstanceBase<ProjectSettings>
    {
        public string serviceUrl;
    }
}

// 资源保存到 Assets/Resources/MyGame/ProjectSettings.asset。
var url = MyGame.ProjectSettings.Instance.serviceUrl;
```

- `ResourcePath` 由类型完整名称生成：命名空间与嵌套类型名转换为目录；`AssetPath` 固定为 `Assets/Resources/<ResourcePath>.asset`。不同命名空间的同名类型不会共用资源。
- `Instance` 第一次访问才读取并缓存持久化资源；缺失时报错，不创建空对象。明确允许缺省的能力使用 `TryGetInstance(out instance)`，仅在缺失时返回 false。
- 错放、重复、子资源副本或指定路径被其他类型占用均报错。编辑器项目资源变化时清除缓存，下次访问重新校验；构建前也校验派生类型的唯一资源配置。运行时仅加载指定 Resources 路径，不扫描整个资源库。
- 资源由 Unity 管理，业务代码借用引用，不销毁它。缓存不会在 Play Mode 切换时重建资源；已由 Unity 销毁的引用会在下次访问重新读取。该入口在主线程调用，不把 Unity 资源加载放到后台线程。
- 此约定保证框架全局入口使用规范资源，不拦截 Unity 自身的 `CreateInstance` / `Instantiate`；临时对象不会自动成为全局实例。不要把业务会话状态、连接、订阅或异步任务放入全局配置资源。

`GameToolSettings` 复用此基类，资源路径及原 GUID 保持不变。工具设置允许没有资源文件，因此使用显式可选读取；接口默认实现规则保持不变。`GameToolSettingsProvider` 每行实现右侧提供“选择”，点击后定位 Project 中的脚本；默认实现也可定位，未配置或失效项不可用。脚本查询只在点击时执行，未找到源码时明确提示。

模块项目设置统一放在 **Project Settings → Verve → Modules → 模块名**。`CoreEditorUtility.ModuleSettingsRoot` 提供公共根路径 `Project/Verve/Modules`；各模块直接注册 Unity 的 `SettingsProvider`，负责自己的绘制、校验与保存，不引入新的设置基类或注册中心。仅包含实际的项目级设置，组件参数仍由 Inspector 编辑；`Tools` 与 `Modules` 同级。

项目新增模块设置可使用相同约定：

```csharp
using UnityEditor;
using Verve.Editor;

internal static class InventorySettingsProvider
{
    [SettingsProvider]
    private static SettingsProvider CreateProvider() =>
        new(CoreEditorUtility.ModuleSettingsRoot + "/Inventory", SettingsScope.Project)
        {
            guiHandler = _ => InventoryEditorSettings.DrawSettings()
        };
}
```

示例中的 `InventoryEditorSettings.DrawSettings()` 由项目提供，负责该模块的设置界面。

配置表设置迁入 **Project Settings → Verve → Modules → Table**，原菜单／主工具栏设置入口打开同一页面；删除 `ConfigTableSettingsWindow`，不保留旧设置路径。纯编辑器配置继续使用 Unity 的 `ScriptableSingleton`，保存在 `ProjectSettings/ConfigTableEditorSettings.asset`，不随 Player 发布。

配置页显示固定的客户端目录、可编辑的 C# 生成目录与服务端表目录。修改保留为草稿，点击“应用”后校验并确认迁移已有输出；“恢复默认”也只修改草稿，不直接改写已保存目录。“还原未应用修改”重新读取保存值；运行模式下禁用修改。没有新增配置文件副本或兼容窗口。

## 引擎与平台边界

- 包最低版本为 Unity 2021.3 LTS，使用 C# 9 和 .NET Standard 2.1。移除低于此版本的循环组件、导入器命名空间和编辑器菜单分支；保留受支持版本、平台与非 Unity 宿主之间的条件编译。
- 保留 Verve 的 `ObjectPool<T>`、`IObjectPool<T>`、`GameObjectPool`，不要求 `UnityEngine.Pool`。
- 保留 PlayerLoop / MonoBehaviour、Unity 对象查询、AssetImporterEditor、UnityWebRequest、协程 / Awaitable、Unity / 非 Unity HTTP 的版本与平台适配。
- Addressables 最低依赖调整为 1.21.19。场景加载使用 1.x、2.x 共有重载；项目可使用更新版本。
- 编辑器 JSON 使用正式的 `com.unity.nuget.newtonsoft-json`，不再依赖 Plastic/Version Control 的私有程序集。显式声明 UGUI、Audio 和 IMGUI 依赖。

## 必须迁移的调用

| 原调用或行为 | 当前契约 |
| --- | --- |
| `PropertyProxy.AddListener` / `RemoveListener` | 使用 `ValueChanged` / `PropertyChanged` 的 `+=`、`-=`；不保留包装方法 |
| 格式化日志没有参数时原样输出格式文本 | 按 `string.Format` 处理；缺少占位参数直接抛出。原样输出含花括号的文本时使用对象重载，如 `Game.Log((object)text)` |
| `using Verve.Network` | `using Verve`；WebSocket 属于 `Verve.Network`，HTTP 工具属于 `Verve.Core` |
| 顶层 `Verve.HttpUtility` 或从 `Verve.Network` 使用 HTTP 工具 | 直接引用 `Verve.Core`，改用 `Game.HttpUtility` |
| `UnityWebRequestUtility.SendAsync(request, ...)` | `Game.HttpUtility.SendAsync(request, ...)`；仅 Unity 平台提供 |
| `UnityWebRequestUtility.GetTextAsync(pathOrUrl, ct)` | `Game.HttpUtility.Get(Game.PathUtility.ToRequestUrl(pathOrUrl), cancellationToken: ct)` |
| `Game.ReflectionUtility.SetPropertyValue(obj, name, value, bindingAttr)` | 普通实例属性使用三参数入口；定制筛选、静态属性或索引器使用 `FindProperty` 返回的 `PropertyInfo` |
| `Game.PathUtility.GetNormalizePath` | `Game.PathUtility.Normalize` |
| Loader 内的路径工具 | `Game.PathUtility.NormalizeProjectPath`、`NormalizeScenePath` |
| 表模块安装后轮询 `IsReady`、`IsLoadSuccessful`、`LoadError` | 等待模块安装；加载错误通过异常传播 |
| `tables.Reload()` | `await tables.ReloadAsync(ct)` |
| 表标量 getter 的 `defaultValue` 参数 | 移除参数；非法值、缺失字段直接报错 |
| 默认加密器与固定密钥 | 使用默认 AES 或在项目工具设置中选择 `ICrypto`；每次调用显式传入密钥 |
| `AESCrypto` | `Game.Crypto.Encrypt` / `Decrypt`；默认 AES 要求传入 64 字节密钥 |
| `Game.RandomUtility` | 按需求直接使用 `UnityEngine.Random` 或 `System.Random` |
| `Utf8String` 的任意编码参数 | 仅接收 UTF-8 文本，拒绝内嵌 NUL |
| HTTP 下载的 `Task<bool>` | `Task`；成功完成或抛出异常 |
| 值类型 `CoroutineOperation` | 拥有取消源的引用句柄；完成或停止后 `IsRunning == false` |

### HTTP 工具

`Game.HttpUtility` 位于 `Runtime/Core/Utilities/Game.HttpUtility.cs` 和 `Game.HttpUtility.Net.cs`，与其他工具一样嵌套在 `Game` 中，无需安装模块。Unity 分支使用 `UnityWebRequest`，非 Unity 分支使用 `HttpClient`；取消、超时和请求失败通过任务异常报告。下载先写同目录临时文件，成功后替换目标，失败或取消时清理临时文件并保留原目标。

`Get`、`PostJson`、`PostForm`、`Put`、`Delete`、`DownloadBytes`、`DownloadFile` 自动释放各自的请求、响应与处理器。非 Unity 分支复用进程生命周期内的 `HttpClient`。Unity 的 `SendAsync` 用于发送自定义请求，只借用传入的实例，结束后仍可读取响应；调用方用 `using` 管理该请求及其处理器。Unity 入口在主线程调用。

删除顶层 `HttpUtility`、`UnityWebRequestUtility` 与重复的 `GetTextAsync`，没有旧类型转发或程序集兼容层。配置表继续依赖 Core，通过路径工具转换本地路径或 StreamingAssets URL 后调用 `Game.HttpUtility.Get`。

### 反射工具

`Game.ReflectionUtility` 提供 `GetFieldValue<T>` / `SetFieldValue`、`GetPropertyValue<T>` / `SetPropertyValue`，简化实例成员查找和读写，包含基类的非公开成员。传入空目标抛出 `ArgumentNullException`，字段或属性缺失分别抛出 `MissingFieldException` / `MissingMemberException`；结果类型转换、索引器参数、只读属性和访问器异常保持反射 API 的错误语义，不返回默认值或尝试其他成员。

`FindField` / `FindProperty` 从指定类型逐级向基类查找，优先返回最近声明；未找到返回 null。默认查找公开和非公开实例成员，可通过 `BindingFlags` 筛选可见性、实例或静态成员；指定 `DeclaredOnly` 时只查本级。层级遍历由工具执行，包含筛选条件允许的基类私有成员；不采用 `FlattenHierarchy` 对私有静态成员的排除规则。

查找返回标准 `FieldInfo` / `PropertyInfo`，可直接用于静态成员、索引器或重复访问。便捷 setter 接收对象实例；修改结构体时应保留并传入同一个装箱对象。工具不持有目标对象，不维护全局成员缓存，不生成动态代码；高频路径可以保存查找结果，方法调用和委托绑定继续使用标准反射 API。裁剪/AOT 构建需按项目配置保留反射访问的成员。

`EnumerateFields` 逐级枚举字段，保留各级同名声明，复用于模块清单的 link.xml 保留图。模块对象引用恢复、按钮参数解析、Unity 属性编辑器字段适配和 UI / Loader / Table 测试统一使用反射工具；已有元数据的读写直接使用该元数据，避免再次查找。

UI 变量、事件代码生成以及模块清单、link.xml 文件写入复用 `Game.FileUtility.WriteAllTextAtomically`，内容未改变时跳过写入和相应重导入；加载器调试器与 UI / Table 资源路径使用 `Game.PathUtility`。物理目录和 Resources 名称中只替换分隔符的两处转换保留原行为，避免路径工具的 `Trim` 改变文件名。

### 共用编辑器工具

通用编辑器能力统一从 `Verve.Editor.CoreEditorUtility` 调用：`DrawButtons` 自动发现目标上的按钮特性，`DrawRowBackground` 绘制列表背景，`FindMonoScriptForType` 按实际类型查询脚本并接受路径筛选，`GetSourcePrefabAsset` 借用场景实例对应的 Prefab 资源，`GetCommandGameObject` 解析菜单对象上下文，`WriteTextAsset` 在内容变化时原子写入并导入资源。删除原 `ButtonEditorHelper` / `EditorListStyle` 类型；UI 类型过滤、变量绑定与模块清单依赖规则仍由各自模块负责。查询返回的脚本和 Prefab 属于资源数据库，工具不销毁借用资源。

列表弹窗统一调用 `CoreEditorUtility.ShowListPopup`，内部实现不公开：

```csharp
CoreEditorUtility.ShowListPopup(anchorRect, assets,
    asset => new GUIContent(asset.name, AssetDatabase.GetAssetPath(asset)),
    asset =>
    {
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    });
```

打开时保存条目与显示内容的快照；公共搜索栏提供 `#`（长度相同，即完整匹配）和 `Aa`（区分大小写），默认均关闭，空白搜索不筛选。搜索条件变化时才更新匹配索引，仅绘制可见行；重新打开时重新查询数据。`matches` 接收 `EditorSearchFilter`，可用其 `Matches` 分别匹配真实名称、路径等字段；`onSearchChanged` 在首次筛选和条件变化时先于匹配调用，适合一次解析查询。移除 `drawSearch`，不再要求调用方设置 `GUI.changed`。配置表编辑窗口也使用 `CoreEditorUtility.DrawSearchToolbar`。

弹窗只拥有显示状态，条目和图标资源均借用；关闭时清除引用与回调，不销毁资源、不建立静态列表缓存。选择先关闭弹窗，再执行回调；业务异常直接传播。Table 与 UI 列表删除重复弹窗实现，保留模块内的数据查询与选择行为。Table 保留全文搜索、长度／大小写选项及打开编辑器时传递搜索条件；文件信息仅在打开列表时读取，解析失败直接报告，不再以空表条目继续显示。

### 共用释放能力与注释约定

- 同步且只需释放一次的对象优先继承 `DisposableObject`，将清理放在 `OnDispose`。基类只保证释放入口执行一次；派生类仍负责资源访问的线程同步。
- `EventDispatcher`、内部订阅句柄、`AssetLoadScope` 和默认模块工厂使用该基类。模块容器的异步卸载、Tick 等待、模块归属校验和加载句柄的释放权转交继续使用专用流程。
- Core 内部的 `ResourceUtility.ReleaseAll` 统一逆序清理资源列表，先移除登记再执行回调，完成全部清理后抛出汇总错误。加载作用域、Loader 资源引用、宿主事件订阅和 UI 部件复用此逻辑，所有权仍由原有框架对象管理。
- UI、Loader 和配置表在取消回调抛错后继续完成各自的等待与清理，再汇总报告错误。UI 和配置表在取消前记录进行中的任务，避免遗漏取消回调同步触发的失败；尚未交给调用方的加载句柄在失败时由框架回收。
- 类型、字段等使用简短的“名称；作用。”；名称已经表达含义时仅写名称。新增方法描述行为并补齐参数、类型参数说明；重写或实现接口的成员只写 `/// <inheritdoc />`。
- 注释中引用具体类型、方法或属性时使用 `<see cref="..."/>`；普通概念、平台名称和示例代码不作为符号引用。

### 容器默认能力与可定制入口

`GameModulesOptions` 集中配置扩展实例的创建。未配置时使用内部的 `GameLoopTickSystemScheduler` 和 `GameModuleFactory`；观察者可选。所有入口接受同一配置：

```csharp
var options = new GameModulesOptions
{
    CreateScheduler = () => new ProjectTickScheduler(),
    CreateModuleFactory = () => new ProjectModuleFactory(),
    CreateObserver = () => new ProjectModuleObserver()
};
var handle = Game.CreateModules(this, options);
await handle.Modules.InstallFromManifestAsync(manifest);
```

- 原 `new GameModules(scheduler)` 迁移为 `new GameModules(options)`；原 `Game.CreateModules(manifest, scheduler)` 迁移为 `Game.CreateModules(manifest, options)`。创建委托每次返回全新实例，不保留直接接收调度器实例的兼容重载。
- 默认调度器和默认模块工厂保持 `internal`；公开接口与配置入口。需要组合默认行为时可保存默认配置的创建委托，通过接口包装每次新建的实例；`ITickOrder` 继续用于系统排序。
- `IGameModuleFactory.Create(Type)` 统一模块创建；默认通过公开无参构造函数创建。`Install<T>`、`Install(Type)`、清单和程序集安装走同一策略，代码泛型入口不再要求 `new()`。显式 `Install(() => ...)` / `manifest.Add<T>(() => ...)` 仍覆盖单次创建。
- `IGameModuleFactory` 增加 `Configure(GameModule)`，自定义工厂直接实现该方法；无需通用配置时方法体可为空。顺序固定为创建 → 框架接管与依赖预留 → 工厂 `Configure` → 条目 `configure` → `OnInstall`。显式条目工厂只覆盖创建，仍执行容器工厂的配置。配置失败释放新实例；借用实例在任何配置之前被拒绝。
- `GameModuleDescriptor.configure` 保留为单条配置，资源清单的字段恢复也使用该路径；字段值可以覆盖工厂提供的通用初值。编辑器预览和构建扫描继续使用内部默认工厂，不调用项目运行时工厂。
- 配置对象可以复用；工厂与调度器实例不能跨容器共享，也不能在释放后复用。空返回值、错误模块类型或已被接管的实例直接报错，不改用默认实现。
- 销毁顺序固定为模块 → 调度器 → 模块工厂 → 观察者。工厂可以管理其专用服务，但不能再次释放已经交给容器的模块。构造阶段失败也会释放此前已接管的扩展。
- 资源清单的运行时创建支持定制，清单类型仍要求公开无参构造函数，以保证编辑器预览、link.xml 扫描和运行时契约一致。仅有注入构造函数的模块使用代码清单；反射创建的类型需按目标平台的裁剪设置保留。

扩展实现仅在容器创建时确定，不支持运行中替换。修改配置只影响之后创建的容器。数据或行为的动态调整由模块、自定义实现提供明确的方法；需要替换实现时结束旧容器再新建。依赖校验、状态转换、回滚、所有权和释放顺序保持框架内固定规则，不提供替换生命周期执行器或注册表的入口。

### 模块操作观察

移除 `OnModulesChanged`、批次合并通知状态和无参数事件调用辅助方法，不保留兼容事件。改用 `GameModulesOptions.CreateObserver` 创建容器独占的 `IGameModuleObserver`，实现 `OnCompleted(GameModuleOperationResult result)` 和 `Dispose()`。

- `GameModuleOperationResult` 包含模块类型、`Install`/`Uninstall` 操作、耗时和 `Failure`。不持有模块实例。`Failure == null` 表示该次模块操作成功；观察者自身的错误不会改写操作结果。
- 通知在单个模块安装尝试完成或实际拆除完成后发送，包含失败清理耗时。无类型工厂在返回实例之前失败时，`ModuleType` 为空。尚未开始的操作（例如前置校验失败、查无待卸载模块）不产生完成通知。
- 清单不再合并为一次集合变化通知。若后续条目失败，前面成功的条目先有安装结果，再有回滚卸载结果。整体清单是否成功仍以安装调用的完成结果为准。
- 回调中禁止嵌套安装、卸载或销毁容器。观察者只接收结果，不参与所有权或生命周期决策。
- 观察者抛出时，框架继续当前清单或清理工作，在本次容器操作出口汇总抛出；不会因为观察者错误回滚成功模块。若操作本身也失败，同时保留原始失败与观察者错误。需要取得容器后处理此类错误时，先创建句柄，再调用安装；`Game.CreateModules(manifest, options)` 创建整体失败时会清理尚未交付的容器。
- 未配置观察者时不创建空实现，不计时。配置了创建委托但返回空或共享实例则直接报错，不禁用观察功能作为兜底。

### 模块所有权与终止语义

- 每个实例只属于一个容器；重复安装相同精确类型直接抛出，旧实例和依赖者保持不变，新建但被拒绝的实例由框架释放。工厂不得返回其他容器拥有的实例。
- 移除运行时自动替换、重装旧实例和卸载失败后的 Tick 恢复。需要更换模块时，先卸载依赖者和目标模块，再安装新的实例。`GameModuleManifest.Add(..., replace: true)` 仅替换尚未执行的清单声明。
- `Uninstall<T>()`、`UninstallAsync<T>(ct)`、`UninstallAll()`、`UninstallAllAsync(ct)` 始终释放模块，不再提供 `dispose: false`。卸载开始后是终止操作：依次停止 Tick、执行卸载、移除登记、释放实例；回调失败也继续清理，最后报告异常。取消只阻止尚未开始拆除的模块。
- 模块保存自己的容器归属，删除全局强引用所有权表。只有所属容器可以释放已接管实例；业务侧直接 `module.Dispose()` 会报错。
- 清单先整理描述符，再逐项调用工厂并立即接管实例。失败或取消后，不会创建后续实例；已安装条目由框架逆序释放。工厂应在调用时创建实例，不要预先创建尚未交给容器的资源。
- 模块的 `OnDispose` 负责最终资源释放，必须能处理安装未完成或卸载钩子失败的状态。同步入口只适用于生命周期回调同步完成的模块；可能等待 I/O 或异步操作时使用 `InstallAsync` / `DisposeAsync`。

模块使用方式统一为业务调用容器安装、框架调用工厂创建、返回后接管实例。自定义工厂和条目工厂内部可以 `new`，用于传入构造参数；框架不通过构造期间的线程标记禁止直接构造。直接创建但尚未交给容器的实例仍由创建者负责。构造函数应只保存参数和建立普通托管状态，资源申请放在接管后的 `OnInstall`，由 `OnDispose` 收尾；构造函数或工厂抛出前未能交付的资源，容器无法代为释放。

所有权链始终是 `GameModulesHandle → GameModules → 模块与扩展实例`，与 Unity 对象无关。`Game.CreateModules()` 可独立使用。需要随 Unity 对象销毁触发自动清理时，可使用可选生命周期绑定，无需自行实现 `OnDestroy`：

```csharp
// 在 MonoBehaviour.Start 等宿主已激活的入口调用。
var handle = Game.CreateModules(this);
await handle.Modules.InstallFromManifestAsync(manifest, cancellationToken);
```

内部适配组件更名为 `GameModulesLifetime`，只把 GameObject 销毁事件转成句柄的 `DisposeAsync` 请求；取消、模块拆除和资源释放全部由框架容器执行。每个 GameObject 同时绑定一个容器的生命周期。首次绑定要求对象处于激活状态，因为 Unity 不保证从未激活的对象收到 `OnDestroy`。不使用 Unity 生命周期绑定时，核心所有权模型保持一致。

### Tween

删除 `TweenExtensions` 的属性动画包装、缓动枚举、Builder、Runner 和播放状态对象，直接迁移到 `Tween.Run`。框架只推进 0～1 进度；属性插值用 Unity 的 `Lerp`，曲线用 `AnimationCurve`，顺序用协程组合。没有旧接口转发层。

```csharp
var start = transform.localPosition;
var curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
yield return Tween.Run(0.25f,
    t => transform.localPosition = Vector3.LerpUnclamped(start, target, curve.Evaluate(t)),
    unscaledTime: true);
```

零时长只应用一次终点。非法时长、取消与回调异常直接报告；协程由调用它的 MonoBehaviour 管理。

### 资源自动释放

常规 Unity 调用优先使用绑定到 Component / GameObject 的加载重载，直接取得资源，不需要保存句柄或维护释放列表：

```csharp
var icon = await loader.LoadAssetAsync<Sprite>("Assets/UI/Icons/Close.png", this, cancellationToken);
```

绑定的 GameObject 销毁时框架自动释放引用。首次绑定同样要求宿主激活。加载期间宿主销毁、作用域释放或请求取消，都由框架释放后来返回的句柄。

高级调用仍可直接使用 `AssetLoadHandle<T>` 或显式 `AssetLoadScope`。`scope.Track(handle)` 现在转交唯一释放权，并使原句柄失效：原句柄不能再次转交，`Result` 不再可读，重复 `Dispose` 不会提前释放作用域拥有的资源。使用 `Track` 返回的资源。释放后的句柄会清除资源与释放委托引用，旧句柄按引用记录身份校验，不能归还新一轮加载的引用。

UI 的页面、ViewPart、Widget 与预制体句柄由 UI 所有者释放；外部提供的 UI 根节点仅被借用。克隆的 GameObject 仍可能共享预制体的材质、纹理等资源，资源所有者的寿命应覆盖使用者。

### UI 模块瘦身

- 移除 View、Widget Inspector 中自带的导航可视化；导航关系可通过 Unity 原生 `Selectable` Inspector 查看和配置。
- `Open` / `OpenAsync` 默认只复用资源路径、显示层和缓存策略相同的唯一实例。配置冲突直接抛出异常，不再隐式销毁旧页面；更换配置时先 `Release(view)` 再打开，需要并存时传入 `openMode: UIViewOpenMode.New`。
- 删除运行时 Prefab 热重载及编辑器导入后的重载提示。修改 Prefab 后重新进入运行模式验证，不保留旧重载入口和失败恢复分支。
- 普通异步打开共享相同请求；取消一个等待者只结束该等待。独立实例分别拥有加载请求，取消后到达的资源句柄由框架回收。
- UI 操作统一在 Unity 主线程执行，包括 `OpeningViewCount`。加载请求在调用加载器前登记；页面正在加载、创建或已经投入使用时，禁止切换根节点。
- 页面记录拥有实例 GameObject 和加载句柄，绑定组件由 View 提供；单独移除绑定组件也会回收框架创建的页面节点。
- 保留单实例复用、多实例、关闭缓存、数量上限淘汰、返回栈、模态遮罩和异步关闭/释放；框架继续管理资源与生命周期，没有新增要求业务维护的管理器或释放句柄。

### UI 打开参数

无参数页面继续继承 `ViewBase`，重写 `OnOpened()`，使用 `ui.Open<HomeView>()` 或 `ui.OpenAsync<HomeView>()`。不声明空参数类，不传 `null` 占位。

有参数页面继承 `ViewBase<TArgs>`，参数派生自 `ViewArgs`，调用只指定页面类型：

```csharp
public sealed class ItemArgs : ViewArgs
{
    public int ItemId { get; }
    public ItemArgs(int itemId) => ItemId = itemId;
}

[ViewConfig("Assets/UI/ItemView.prefab")]
public sealed class ItemView : ViewBase<ItemArgs>
{
    protected override void OnOpened(ItemArgs args)
    {
        // 展示 args.ItemId；也可从 OpenArgs 读取本次参数。
    }
}

var args = new ItemArgs(itemId);
var item = ui.Open<ItemView>(args);
ui.Close(item);
item = await ui.OpenAsync<ItemView>(args); // 复用参数和关闭后的缓存实例。
```

- `IUIManager` 保持同步／异步、特性配置／显式路径共 4 个打开方法，有参和无参共用可选 `ViewArgs args`；无需额外传入参数类型。
- 参数建议使用只读字段或只读属性，可直接借用业务模型或列表；只读约束针对参数字段，不要求借用的模型不可变。参数对象按引用传递，不装箱、不复制；新建对象有一次分配，复用同一实例是可选优化，无需维护参数池或缓存。
- 参数类型关系由框架在加载前校验，继承链结果按页面类型缓存；类型错误、必需参数为空、无参数页面多传参数都会抛错。这是运行时校验，不宣称调用点具备参数配对的编译期约束。
- `OpenArgs` 是强类型只读属性，关闭、释放、打开失败时自动清空。异步操作结束也会清空参数引用；框架不销毁或释放借用的业务数据。
- 已打开页面和合并中的异步请求须使用同一参数实例，避免静默忽略新参数。关闭后可带新参数打开；需要并存时传 `openMode: UIViewOpenMode.New`。
- `ControllerBase<TView, TModel>.OnOpened()` 不再接收 `object`，从具体 `View.OpenArgs` 读取参数。异步打开后回调改为 `(view, ct)`，同样读取页面参数。
- 删除原 `object data` 接口、`OpenData`、`OnOpened(object)`；`OpenNew` / `OpenNewAsync` 合并到 `Open` / `OpenAsync` 的 `openMode` 参数，不保留旧入口。项目中的 `TestView` 使用嵌套只读 `Args`，现有生成绑定不变。

### 异步模块安装

```csharp
var manifest = new GameModuleManifest();
manifest.Add<ConfigTableModule>();

var handle = Game.CreateModules();
try
{
    await handle.Modules.InstallFromManifestAsync(manifest, cancellationToken);
    var tables = handle.Modules.GetModule<IConfigTables>();
    var table = tables.Get("Item");
    await tables.ReloadAsync(cancellationToken);
}
finally
{
    await handle.DisposeAsync();
}
```

表模块不再依赖 Network，直接使用 Core 的 UnityWebRequest 适配读取本地文件、Android StreamingAssets 或 URL。重载期间不可查询旧数据，完整校验后才发布新表。空字符串与空数组合法，空数字、空布尔值及无效格式会在数据校验时失败。

### 事件

`Game.On/Off/Emit` 的字符串和整数入口保留，但内部键变为 `EventKey`。字符串不再转成整数哈希，消除字符串碰撞及字符串/整数事件串线。订阅快照允许回调期间注销，注销旧令牌不会移除同一委托的新注册。回调异常直接传播并停止本次后续回调。

删除 `Emit` 的可选调用文件/行号参数，它们会与字符串消息参数产生重载歧义。调试记录保留消息参数，不再记录调用文件与行号。使用公开 `EventHandlers` 字典或自定义调试器的代码需要迁移到 `EventKey`。

### 对象池

`Capacity` 表示最多保留的闲置对象数量，不限制借出数量；容量为零仍允许创建对象。池本身不提供线程同步。`TryGet` 筛选未命中返回 false，`Get` 未命中抛出。预热参数错误不再被自动夹取到合法范围。调试构建检测重复归还同一个引用；相等的值类型仍可以入池。

借出回调失败会销毁相应对象。清空、缩容、释放会继续处理其余对象，随后汇总清理异常。`GameObjectPool.TryGet` 的带位置重载保持相同语义。

### 加密

`AesCrypto` 使用 AES-256-CBC 和 HMAC-SHA256，64 字节密钥分别用于加密和认证，每次加密生成独立 IV。密文为 `IV || ciphertext || HMAC`，先验证认证码再解密，不读取旧密文格式。密钥由项目配置和保存，框架不生成可预测的默认密钥。


## 职责与失败处理

- Core 统一引用相等比较、清理异常汇总、等待任务的取消、路径规范化、原子文本写入和 UnityWebRequest 等待。
- `WaitWithCancellationAsync` 只取消当前等待，不取消共享操作；旧运行时保留必要实现。
- AssetBundle 必须同时包含 Unity 构建清单和框架生成的 `verve-bundles.json`；运行时直接读取目录，不扫描目录或逐个加载资源包建立索引。
- UI 的 View、ViewPart、Widget 保持各自生命周期边界；允许可构造的嵌套业务类型。事件通知和资源清理错误向调用方传播。
- 单例组件发现多个实例时直接报错，不再静默销毁其他 GameObject。
- 编辑器不再猜测脚本来源、将损坏的模块数据替换成默认实例，或在配置损坏后重置输出目录。CSV 使用严格 UTF-8 或 BOM 明确指定的 Unicode 编码。
- Unity 编辑器导入回调仍通过导入错误面板报告失败；这属于编辑器边界的错误呈现。
