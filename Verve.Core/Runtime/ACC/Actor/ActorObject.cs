#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    ///   <para>Unity 行动者对象；创建并装配由当前对象驱动的 <see cref="Actor"/>。</para>
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Verve/Actor Object")]
    public sealed class ActorObject : MonoBehaviour, IActorObject
    {
        /// <summary>
        ///   <para>可选目标世界名称；为空时要求项目中只有一个活跃世界。</para>
        /// </summary>
        [SerializeField, Tooltip("可选目标世界名称；为空时使用唯一的活跃世界。")]
        private string m_WorldName;
        /// <summary>
        ///   <para>创建 Actor 后应用的初始表单；能力和组件在表单资产中配置。</para>
        /// </summary>
        [SerializeField, Tooltip("创建 Actor 后应用的初始能力表单。")]
        private CapabilitySheetAsset m_InitialSheet;
        /// <summary>
        ///   <para>当前对象禁用的表单能力；不会修改初始表单资产。</para>
        /// </summary>
        [SerializeField, Tooltip("仅禁用当前 Actor 的表单能力，不修改初始表单资产。")]
        private List<string> m_DisabledCapabilityTypeNames = new();

        /// <summary>
        ///   <para>当前所属世界；未创建时为空。</para>
        /// </summary>
        [NonSerialized] private World m_World;
        /// <summary>
        ///   <para>当前行动者句柄。</para>
        /// </summary>
        [NonSerialized] private Actor m_Actor = Actor.none;
        /// <summary>
        ///   <para>是否正在执行创建流程。</para>
        /// </summary>
        [NonSerialized] private bool m_IsCreating;
        /// <summary>
        ///   <para>是否在创建过程中收到销毁回调。</para>
        /// </summary>
        [NonSerialized] private bool m_DestroyRequested;
        /// <summary>
        ///   <para>是否已完成世界登记。</para>
        /// </summary>
        [NonSerialized] private bool m_IsAttached;
        /// <summary>
        ///   <para>最近一次创建失败信息；成功后清空。</para>
        /// </summary>
        [NonSerialized] private string m_CreateError;

        /// <summary>
        ///   <para>当前行动者；尚未创建时返回 <see cref="Actor.none"/>。</para>
        /// </summary>
        public Actor Actor => m_Actor;
        /// <summary>
        ///   <para>当前所属世界；尚未创建时返回 <see langword="null"/>。</para>
        /// </summary>
        public World World => m_World;
        /// <summary>
        ///   <para>当前是否已创建且仍然有效。</para>
        /// </summary>
        public bool IsCreated => m_IsAttached && m_World != null && !m_Actor.IsNone && m_World.IsActorAlive(m_Actor);
        /// <summary>
        ///   <para>可选目标世界名称；为空时使用唯一活跃世界。</para>
        /// </summary>
        public string WorldName => m_WorldName;
        /// <summary>
        ///   <para>创建时应用的初始表单。</para>
        /// </summary>
        public CapabilitySheetAsset InitialSheet => m_InitialSheet;
        /// <summary>
        ///   <para>最近一次创建失败信息；没有失败时为空。</para>
        /// </summary>
        public string CreateError => m_CreateError;

        /// <summary>
        ///   <para>对象首次启用时创建行动者。</para>
        /// </summary>
        private void Start() => Create();

        /// <summary>
        ///   <para>按序列化配置解析世界并创建行动者。</para>
        /// </summary>
        public void Create()
        {
            try { Create(ResolveWorld(m_WorldName)); }
            catch (Exception exception)
            {
                m_CreateError = FormatCreateError(exception);
                throw;
            }
        }

        /// <summary>
        ///   <para>在指定世界中创建并装配行动者。</para>
        /// </summary>
        /// <param name="world">目标世界。</param>
        public void Create(World world)
        {
            try { CreateImpl(world); }
            catch (Exception exception)
            {
                m_CreateError = FormatCreateError(exception);
                throw;
            }
        }

        /// <summary>
        ///   <para>执行 Actor 创建和装配。</para>
        /// </summary>
        /// <param name="world">目标世界。</param>
        private void CreateImpl(World world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            Game.ThrowIfNotOnMainThread($"{nameof(ActorObject)}.{nameof(Create)}");
            if (m_IsCreating)
                throw new InvalidOperationException($"{nameof(ActorObject)} is already creating an actor.");
            if (m_DestroyRequested)
                throw new InvalidOperationException($"{nameof(ActorObject)} is being destroyed.");
            if (m_World != null)
            {
                if (ReferenceEquals(m_World, world) && m_IsAttached && IsCreated) return;
                throw new InvalidOperationException($"{nameof(ActorObject)} already has an actor.");
            }

            m_IsCreating = true;
            var actor = Actor.none;
            try
            {
                actor = world.CreateActor();
                m_World = world;
                m_Actor = actor;

                var sheet = BuildConfiguredSheet();
                if (sheet != null)
                    world.ApplySheet(actor, sheet);

                if (m_DestroyRequested)
                    throw new InvalidOperationException($"{nameof(ActorObject)} was destroyed while creating its actor.");

                world.AttachActorObject(actor, this, ClearReferences);
                m_IsAttached = true;
                m_CreateError = null;
            }
            catch (Exception createException)
            {
                Exception cleanupException = null;
                try
                {
                    if (!actor.IsNone && world.IsActorAlive(actor))
                        world.DestroyActor(actor);
                }
                catch (Exception exception)
                {
                    cleanupException = exception;
                }

                // 更新期间的销毁会延迟到当前 Tick 结束；清理命令已经归世界所有。
                if (cleanupException == null || !world.IsActorAlive(actor) || world.Capabilities.IsUpdating)
                    ClearReferencesAfterCreateFailure();

                if (cleanupException != null)
                    throw new AggregateException(
                        $"Creating {nameof(ActorObject)} failed and actor cleanup also failed.",
                        createException,
                        cleanupException);
                throw;
            }
            finally
            {
                m_IsCreating = false;
            }
        }

        /// <summary>
        ///   <para>对象销毁时请求世界释放行动者或解除外部登记。</para>
        /// </summary>
        private void OnDestroy()
        {
            if (m_IsCreating)
            {
                m_DestroyRequested = true;
                return;
            }

            var world = m_World;
            var actor = m_Actor;
            if (world == null || actor.IsNone)
            {
                ClearReferences();
                return;
            }

            if (!world.IsActorAlive(actor))
            {
                ClearReferences();
                return;
            }

            try
            {
                // 复制层拥有的行动者必须由连接释放；对象销毁只解除外部登记。
                if (world.Replication?.Owns(actor) == true)
                    world.DetachActorObject(this);
                else
                    world.DestroyActor(actor);
            }
            catch (Exception destroyException)
            {
                if (!world.IsActorAlive(actor))
                {
                    ClearReferences();
                    throw;
                }

                // 销毁失败时仍解除 Unity 对象登记，避免世界持有已销毁的对象引用。
                try { world.DetachActorObject(this); }
                catch (Exception detachException)
                {
                    throw new AggregateException(
                        $"Destroying {nameof(ActorObject)} failed and detaching it also failed.",
                        destroyException,
                        detachException);
                }
                throw;
            }

            // 能力更新期间会延迟销毁；此后句柄由世界继续持有并最终清理。
            ClearReferences();
        }

        /// <summary>
        ///   <para>清除当前运行时关联。</para>
        /// </summary>
        private void ClearReferences()
        {
            m_World = null;
            m_Actor = Actor.none;
            m_IsAttached = false;
            m_CreateError = null;
        }

        /// <summary>
        ///   <para>清除创建失败后的运行时关联并保留错误信息。</para>
        /// </summary>
        private void ClearReferencesAfterCreateFailure()
        {
            m_World = null;
            m_Actor = Actor.none;
            m_IsAttached = false;
        }

        /// <summary>
        ///   <para>构建当前对象专属的表单副本。</para>
        /// </summary>
        /// <returns>应用禁用项后的表单；没有初始表单时返回 <see langword="null"/>。</returns>
        private CapabilitySheet BuildConfiguredSheet()
        {
            if (m_InitialSheet == null)
            {
                if (m_DisabledCapabilityTypeNames != null && m_DisabledCapabilityTypeNames.Count > 0)
                    throw new InvalidOperationException(
                        "Disabled capabilities require an initial capability sheet.");
                return null;
            }

            var disabledTypes = new HashSet<Type>();
            if (m_DisabledCapabilityTypeNames != null)
            {
                for (var i = 0; i < m_DisabledCapabilityTypeNames.Count; i++)
                {
                    var disabledType = ResolveType(m_DisabledCapabilityTypeNames[i]);
                    ValidateCapabilityType(disabledType, i);
                    if (!disabledTypes.Add(disabledType))
                        throw new InvalidOperationException($"Capability {disabledType.FullName} is disabled more than once.");
                }
            }

            var source = m_InitialSheet.ToSheet();
            if (disabledTypes.Count == 0)
                return source;

            var matched = new HashSet<Type>();
            var result = CloneSheet(source, disabledTypes, matched);
            foreach (var disabledType in disabledTypes)
            {
                if (!matched.Contains(disabledType))
                    throw new InvalidOperationException(
                        $"Disabled capability {disabledType.FullName} was not found in the initial sheet.");
            }
            return result;
        }

        /// <summary>
        ///   <para>复制表单结构并移除禁用能力。</para>
        /// </summary>
        /// <param name="source">源表单。</param>
        /// <param name="disabledTypes">禁用能力类型。</param>
        /// <param name="matched">已匹配的禁用类型。</param>
        /// <returns>复制后的表单。</returns>
        private static CapabilitySheet CloneSheet(
            CapabilitySheet source,
            HashSet<Type> disabledTypes,
            HashSet<Type> matched)
        {
            var result = new CapabilitySheet();
            for (var i = 0; i < source.ComponentTypes.Count; i++)
                result.AddComponent(source.ComponentTypes[i]);

            for (var i = 0; i < source.CapabilityTypes.Count; i++)
            {
                var type = source.CapabilityTypes[i];
                if (disabledTypes.Contains(type))
                {
                    matched.Add(type);
                    continue;
                }
                result.AddCapability(type);
            }

            for (var i = 0; i < source.SubSheets.Count; i++)
                result.AddSubSheet(CloneSheet(source.SubSheets[i], disabledTypes, matched));
            return result;
        }

        /// <summary>
        ///   <para>验证能力类型。</para>
        /// </summary>
        /// <param name="type">待验证类型。</param>
        /// <param name="index">禁用项索引。</param>
        private static void ValidateCapabilityType(Type type, int index)
        {
            if (type == null || !typeof(Capability).IsAssignableFrom(type) || type.IsAbstract ||
                type.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException(
                    $"Disabled capability entry {index} has an invalid capability type.");
        }

        /// <summary>
        ///   <para>解析程序集限定类型名。</para>
        /// </summary>
        /// <param name="assemblyQualifiedName">程序集限定类型名。</param>
        /// <returns>解析到的类型；名称无效时返回 <see langword="null"/>。</returns>
        private static Type ResolveType(string assemblyQualifiedName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
                return null;
            try
            {
                return Type.GetType(assemblyQualifiedName, false);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        ///   <para>格式化创建失败信息。</para>
        /// </summary>
        /// <param name="exception">创建异常。</param>
        /// <returns>用于 Inspector 的简短错误信息。</returns>
        private static string FormatCreateError(Exception exception)
            => $"{exception.GetType().Name}: {exception.Message}";

        /// <summary>
        ///   <para>从当前模块容器中解析目标世界。</para>
        /// </summary>
        /// <param name="worldName">目标世界名称。</param>
        /// <returns>唯一匹配的世界。</returns>
        private static World ResolveWorld(string worldName)
        {
            Game.ThrowIfNotOnMainThread(nameof(ResolveWorld));
            var handles = new List<GameModulesHandle>(4);
            Game.CopyModuleHandlesTo(handles);

            var hasWorldName = !string.IsNullOrWhiteSpace(worldName);
            World result = null;
            var matchCount = 0;
            for (var i = 0; i < handles.Count; i++)
            {
                if (!handles[i].TryGetModules(out var modules) ||
                    !modules.TryGetModule<IWorldManager>(out var manager))
                    continue;

                var candidate = hasWorldName ? manager.Get(worldName) : manager.Active;
                if (candidate == null) continue;
                if (++matchCount > 1)
                    throw new InvalidOperationException(
                        $"Multiple worlds match '{(hasWorldName ? worldName : "the active world")}'. " +
                        $"Assign a unique {nameof(ActorObject)} world name.");
                result = candidate;
            }

            return result ?? throw new InvalidOperationException(
                $"No world matches '{(hasWorldName ? worldName : "the active world")}'. " +
                $"Install {nameof(WorldManagerModule)} and create the world before {nameof(ActorObject)} starts.");
        }
    }
}

#endif
