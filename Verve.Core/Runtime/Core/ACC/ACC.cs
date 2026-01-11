// ================================================
// Actor-Component-Capability Architecture
// ================================================

namespace Verve
{
    using System;
    using System.Buffers;
    using System.Threading;
    using System.Reflection;
    using System.Diagnostics;
    using System.Buffers.Binary;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    
    
    #region 基础类型定义
    
    /// <summary>
    ///   <para>行动者</para>
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential), DebuggerDisplay("{ToString}")]
    public readonly struct Actor : IEquatable<Actor>, IComparable<Actor>
    {
        private const long NONE_ID = -1L;
        private const uint INDEX_MASK = 0x7FFFFFFFU;
        private const uint VERSION_MASK = 0x7FFFFFFFU;
        private const uint MIN_VERSION = 1U;

        /// <summary>
        ///   <para>空行动者标识符</para>
        /// </summary>
        public static readonly Actor none = new Actor(NONE_ID);
        
        /// <summary>
        ///   <para>行动者标识符（64位：高32位索引，低32位版本号）</para>
        /// </summary>
        public readonly long id;

        /// <summary>
        ///   <para>行动者索引</para>
        /// </summary>
        public int Index { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (int)((uint)(id >> 32) & INDEX_MASK); }
        /// <summary>
        ///   <para>行动者版本</para>
        /// </summary>
        public int Version { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (int)((uint)id & VERSION_MASK); }
        
        /// <summary>
        ///   <para>是否为空行动者</para>
        /// </summary>
        public bool IsNone { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => id == NONE_ID;}

        public Actor(long id) => this.id = id;
        public Actor(int index, int version)
        {
            if ((uint)index > INDEX_MASK || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if ((uint)version > VERSION_MASK || version < MIN_VERSION)
                throw new ArgumentOutOfRangeException(nameof(version));

            id = ((long)index << 32) | (uint)version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(Actor other) => id == other.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is Actor other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(Actor other) => id.CompareTo(other.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Actor left, Actor right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Actor left, Actor right) => !left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator long(Actor actor) => actor.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => id.GetHashCode();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => IsNone ? "Actor(None)" : $"Actor({Index}:{Version})";
    }

    /// <summary>
    ///   <para>组件类型标识符</para>
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public readonly struct ComponentTypeId : IEquatable<ComponentTypeId>, IComparable<ComponentTypeId>
    {
        /// <summary>
        ///   <para>空组件类型标识符</para>
        /// </summary>
        public static readonly ComponentTypeId none = new ComponentTypeId(0);
        private readonly int m_Value;

        public ComponentTypeId(int value) => m_Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ComponentTypeId Create<T>() where T : struct, IComponent
            => ComponentTypeRegistry<T>.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(ComponentTypeId other) => m_Value == other.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is ComponentTypeId other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(ComponentTypeId other) => m_Value.CompareTo(other.m_Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(ComponentTypeId left, ComponentTypeId right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(ComponentTypeId left, ComponentTypeId right) => !left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(ComponentTypeId componentTypeId) => componentTypeId.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => m_Value == 0 ? "ComponentType(None)" : $"ComponentType({m_Value})";
    }

    /// <summary>
    ///   <para>能力类型标识符</para>
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public readonly struct CapabilityTypeId : IEquatable<CapabilityTypeId>, IComparable<CapabilityTypeId>
    {
        public static readonly CapabilityTypeId none = new CapabilityTypeId(0);
        private readonly int m_Value;

        public CapabilityTypeId(int value) => m_Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CapabilityTypeId Create<T>() where T : Capability
            => CapabilityTypeRegistry<T>.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(CapabilityTypeId other) => m_Value == other.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is CapabilityTypeId other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(CapabilityTypeId other) => m_Value.CompareTo(other.m_Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CapabilityTypeId left, CapabilityTypeId right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CapabilityTypeId left, CapabilityTypeId right) => !left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(CapabilityTypeId capabilityTypeId) => capabilityTypeId.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => m_Value == 0 ? "CapabilityType(None)" : $"CapabilityType({m_Value})";
    }

    /// <summary>
    ///   <para>标签标识符（用于能力阻塞机制）</para>
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public readonly struct TagId : IEquatable<TagId>, IComparable<TagId>
    {
        public static readonly TagId none = new TagId(0);
        private readonly int m_Value;

        public TagId(int value) => m_Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(TagId other) => m_Value == other.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is TagId other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(TagId other) => m_Value.CompareTo(other.m_Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(TagId left, TagId right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(TagId left, TagId right) => !left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(TagId tagId) => tagId.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => m_Value == 0 ? "Tag(None)" : $"Tag({m_Value})";
    }

    #endregion
    
    #region 组件系统

    /// <summary>
    ///   <para>组件标记接口（纯数据容器，必须为结构体）</para>
    /// </summary>
    public interface IComponent { }

    /// <summary>
    ///   <para>组件类型注册表</para>
    /// </summary>
    internal static class ComponentTypeRegistry
    {
        [ThreadStatic] private static int s_NextTypeId = 1;
        private static readonly Dictionary<Type, ComponentTypeId> s_TypeToId = new(128);
        private static readonly Dictionary<int, Type> s_IdToType = new(128);
        private static readonly object s_Lock = new();

        public static int TypeCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { lock (s_Lock) return s_TypeToId.Count; }
        }

        public static ComponentTypeId GetTypeId(Type componentType)
        {
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));
            if (!typeof(IComponent).IsAssignableFrom(componentType))
                throw new ArgumentException($"Type {componentType.Name} must implement IComponent");
            if (!componentType.IsValueType)
                throw new ArgumentException($"Component {componentType.Name} must be a struct");

            if (s_TypeToId.TryGetValue(componentType, out var typeId))
                return typeId;

            lock (s_Lock)
            {
                if (s_TypeToId.TryGetValue(componentType, out typeId))
                    return typeId;

                var id = Interlocked.Increment(ref s_NextTypeId) - 1;
                typeId = new ComponentTypeId(id);

                s_TypeToId[componentType] = typeId;
                s_IdToType[id] = componentType;

                return typeId;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType(int typeId)
        {
            lock (s_Lock) { return s_IdToType.TryGetValue(typeId, out var type) ? type : null; }
        }

        public static void PreRegister(params Type[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0) return;

            lock (s_Lock)
            {
                foreach (var type in componentTypes)
                {
                    if (type != null && !s_TypeToId.ContainsKey(type))
                        GetTypeId(type);
                }
            }
        }

        [Conditional("DEBUG"), Conditional("UNITY_EDITOR")]
        public static void Clear()
        {
            lock (s_Lock)
            {
                s_TypeToId.Clear();
                s_IdToType.Clear();
                s_NextTypeId = 1;
            }
        }
    }

    /// <summary>
    ///   <para>组件类型注册表</para>
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    internal static class ComponentTypeRegistry<T> where T : struct, IComponent
    {
        public static readonly ComponentTypeId id = ComponentTypeRegistry.GetTypeId(typeof(T));
    }

    #endregion
    
    #region 能力系统
    
    /// <summary>
    ///   <para>能力更新分组（用于控制能力执行阶段与顺序）</para>
    /// </summary>
    public enum TickGroup
    {
        /// <summary>
        ///   <para>早期更新</para>
        /// </summary>
        Early,
        /// <summary>
        ///   <para>物理更新</para>
        /// </summary>
        Physics,
        /// <summary>
        ///   <para>游戏逻辑更新</para>
        /// </summary>
        Gameplay,
        /// <summary>
        ///   <para>延迟更新</para>
        /// </summary>
        Late
    }
    
    /// <summary>
    ///   <para>能力基类（只执行逻辑，不存储数据）</para>
    /// </summary>
    [Serializable, DebuggerDisplay("{ToString}")]
    public abstract class Capability : IDisposable
    {
        private const TickGroup DEFAULT_TICK_GROUP = TickGroup.Gameplay;
        
        private Actor m_OwnerActor;
        private World m_OwnerWorld;
        private TagId[] m_Tags = Array.Empty<TagId>();
        private ComponentMask m_RequiredComponents;
        private ComponentMask m_BlockedComponents;

        /// <summary>
        ///   <para>能力拥有者</para>
        /// </summary>
        public Actor OwnerActor { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_OwnerActor; }
        /// <summary>
        ///   <para>能力所在世界</para>
        /// </summary>
        public World OwnerWorld { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_OwnerWorld; }
        /// <summary>
        ///   <para>是否激活</para>
        /// </summary>
        public bool IsActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] internal set;
        }
        /// <summary>
        ///   <para>更新分组（决定执行阶段）</para>
        /// </summary>
        public virtual TickGroup TickGroup { get; internal set; } = DEFAULT_TICK_GROUP;
        /// <summary>
        ///   <para>组内执行顺序（数值越小越先执行）</para>
        /// </summary>
        public virtual int TickOrder { get; internal set; }
        /// <summary>
        ///   <para>是否已释放</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] private set;
        }
        /// <summary>
        ///   <para>标签集合（用于能力间阻塞/解锁控制）</para>
        /// </summary>
        public ReadOnlySpan<TagId> Tags => m_Tags;
        /// <summary>
        ///   <para>所需组件掩码（拥有这些组件方可激活）</para>
        /// </summary>
        public ref readonly ComponentMask RequiredComponents => ref m_RequiredComponents;
        /// <summary>
        ///   <para>阻塞组件掩码（拥有这些组件则阻止激活）</para>
        /// </summary>
        public ref readonly ComponentMask BlockedComponents => ref m_BlockedComponents;
        /// <summary>
        ///   <para>能力类型标识符</para>
        /// </summary>
        public CapabilityTypeId TypeId => CapabilityTypeRegistry.GetTypeId(GetType());

        /// <summary>
        ///   <para>设置（仅在添加后执行一次）</para>
        /// </summary>
        protected virtual void OnSetup() { }
        
        /// <summary>
        ///   <para>判断是否应该激活</para>
        /// </summary>
        protected internal virtual bool ShouldActivate()
        {
            var actorMask = m_OwnerWorld.Actors.GetComponentMask(m_OwnerActor);
            return actorMask.ContainsAll(m_RequiredComponents) &&
                   actorMask.ContainsNone(m_BlockedComponents);
        }
        
        /// <summary>
        ///   <para>当被激活时执行</para>
        /// </summary>
        protected internal virtual void OnActivated() { }

        /// <summary>
        ///   <para>当被激活时执行每帧调用</para>
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        protected internal virtual void TickActive(in float deltaTime) { }
        
        /// <summary>
        ///   <para>判断是否应该被失活</para>
        /// </summary>
        protected internal virtual bool ShouldDeactivate()
        {
            var actorMask = m_OwnerWorld.Actors.GetComponentMask(m_OwnerActor);
            return !actorMask.ContainsAll(m_RequiredComponents) ||
                   actorMask.ContainsAny(m_BlockedComponents);
        }
        
        /// <summary>
        ///   <para>当被失活时执行</para>
        /// </summary>
        protected internal virtual void OnDeactivated() { }

        /// <summary>
        ///   <para>依赖组件</para>
        /// </summary>
        protected void Require<T>() where T : struct, IComponent
            => m_RequiredComponents.Add(ComponentTypeRegistry<T>.id);

        /// <summary>
        ///   <para>阻塞组件</para>
        /// </summary>
        protected void Block<T>() where T : struct, IComponent
            => m_BlockedComponents.Add(ComponentTypeRegistry<T>.id);

        /// <summary>
        ///   <para>依赖组件</para>
        /// </summary>
        protected void Require(Type componentType)
            => m_RequiredComponents.Add(ComponentTypeRegistry.GetTypeId(componentType));

        /// <summary>
        ///   <para>阻塞组件</para>
        /// </summary>
        protected void Block(Type componentType)
            => m_BlockedComponents.Add(ComponentTypeRegistry.GetTypeId(componentType));

        /// <summary>
        ///   <para>添加标签</para>
        /// </summary>
        protected void AddTag(TagId tagId)
        {
            var newTags = new TagId[m_Tags.Length + 1];
            m_Tags.CopyTo(newTags, 0);
            newTags[^1] = tagId;
            m_Tags = newTags;
        }

        /// <summary>
        ///   <para>批量添加标签</para>
        /// </summary>
        protected void AddTags(ReadOnlySpan<TagId> tags)
        {
            if (tags.Length == 0) return;
            var newTags = new TagId[m_Tags.Length + tags.Length];
            m_Tags.CopyTo(newTags, 0);
            for (int i = 0; i < tags.Length; i++)
                newTags[m_Tags.Length + i] = tags[i];
            m_Tags = newTags;
        }

        /// <summary>
        ///   <para>判断是否拥有标签</para>
        /// </summary>
        protected bool HasTag(TagId tagId)
        {
            for (int i = 0; i < m_Tags.Length; i++)
                if (m_Tags[i] == tagId) return true;
            return false;
        }
        
        /// <summary>
        ///   <para>添加标签</para>
        /// </summary>
        protected void AddTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;
            AddTag(TagRegistry.GetTagId(tagName));
        }
    
        /// <summary>
        ///   <para>添加多个标签</para>
        /// </summary>
        protected void AddTags(params string[] tagNames)
        {
            var tagIds = new TagId[tagNames.Length];
            for (int i = 0; i < tagNames.Length; i++)
                tagIds[i] = TagRegistry.GetTagId(tagNames[i]);
            AddTags(tagIds);
        }
    
        /// <summary>
        ///   <para>检查标签是否被阻塞</para>
        /// </summary>
        protected bool IsTagBlocked(string tagName) => !string.IsNullOrWhiteSpace(tagName) && IsTagBlocked(TagRegistry.GetTagId(tagName));
    
        /// <summary>
        ///   <para>阻塞指定标签的能力</para>
        /// </summary>
        protected void BlockCapabilitiesWithTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;
            BlockCapabilitiesWithTag(TagRegistry.GetTagId(tagName));
        }
    
        /// <summary>
        ///   <para>解除阻塞指定标签的能力</para>
        /// </summary>
        protected void UnblockCapabilitiesWithTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;
            UnblockCapabilitiesWithTag(TagRegistry.GetTagId(tagName));
        }

        /// <summary>
        ///   <para>清空标签</para>
        /// </summary>
        protected void ClearTags()
        {
            m_Tags = Array.Empty<TagId>();
        }

        /// <summary>
        ///   <para>阻塞标签</para>
        /// </summary>
        protected void BlockCapabilitiesWithTag(TagId tagId)
        {
            m_OwnerWorld.Capabilities.BlockTag(m_OwnerActor, tagId, this);
        }

        /// <summary>
        ///   <para>解除阻塞标签</para>
        /// </summary>
        protected void UnblockCapabilitiesWithTag(TagId tagId)
        {
            m_OwnerWorld.Capabilities.UnblockTag(m_OwnerActor, tagId, this);
        }

        /// <summary>
        ///   <para>批量阻塞标签</para>
        /// </summary>
        protected void BlockCapabilitiesWithTags(ReadOnlySpan<TagId> tags)
        {
            for (int i = 0; i < tags.Length; i++)
                m_OwnerWorld.Capabilities.BlockTag(m_OwnerActor, tags[i], this);
        }

        /// <summary>
        ///   <para>批量解除阻塞标签</para>
        /// </summary>
        protected void UnblockCapabilitiesWithTags(ReadOnlySpan<TagId> tags)
        {
            for (int i = 0; i < tags.Length; i++)
                m_OwnerWorld.Capabilities.UnblockTag(m_OwnerActor, tags[i], this);
        }

        /// <summary>
        ///   <para>判断标签是否被阻塞</para>
        /// </summary>
        protected bool IsTagBlocked(TagId tagId)
        {
            return m_OwnerWorld.Capabilities.IsTagBlocked(m_OwnerActor, tagId);
        }

        /// <summary>
        ///   <para>判断标签是否被阻塞</para>
        /// </summary>
        protected bool AnyTagBlocked(ReadOnlySpan<TagId> tags)
        {
            for (int i = 0; i < tags.Length; i++)
                if (m_OwnerWorld.Capabilities.IsTagBlocked(m_OwnerActor, tags[i]))
                    return true;
            return false;
        }

        /// <summary>
        ///   <para>设置执行顺序</para>
        /// </summary>
        protected void SetTick(TickGroup group, int order)
        {
            TickGroup = group;
            TickOrder = order;
        }

        /// <summary>
        ///   <para>重置状态</para>
        /// </summary>
        protected void ResetState()
        {
            IsActive = false;
            IsDisposed = false;
            m_RequiredComponents.Clear();
            m_BlockedComponents.Clear();
            m_Tags = Array.Empty<TagId>();
            TickGroup = DEFAULT_TICK_GROUP;
            TickOrder = 0;
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            if (IsActive)
            {
                try { OnDeactivated(); }
                catch { }
                IsActive = false;
            }

            m_RequiredComponents.Clear();
            m_BlockedComponents.Clear();
            m_Tags = Array.Empty<TagId>();
            m_OwnerActor = default;
            m_OwnerWorld = null;
            Dispose(true);
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   <para>释放资源</para>
        /// </summary>
        protected virtual void Dispose(bool disposing) { }
        
        /// <summary>
        ///   <para>设置能力实例</para>
        /// </summary>
        internal void Setup(Actor actor, World world)
        {
            m_OwnerActor = actor;
            m_OwnerWorld = world;
            ResetState();
            OnSetup();
        }

        ~Capability() => Dispose();

        public override string ToString() => $"{GetType().Name} (Owner: {m_OwnerActor}, Active: {IsActive}, Group: {TickGroup}, Order: {TickOrder})";
    }
    
    /// <summary>
    ///   <para>能力实例</para>
    /// </summary>
    internal struct CapabilityInstance
    {
        public Capability capability;
        public int tickGroup;
        public int tickOrder;
    }

    /// <summary>
    ///   <para>能力类型注册表</para>
    /// </summary>
    internal static class CapabilityTypeRegistry
    {
        [ThreadStatic] private static int s_NextTypeId = 1;
        private static readonly Dictionary<Type, CapabilityTypeId> s_TypeToId = new(256);
        private static readonly Dictionary<int, Type> s_IdToType = new(256);
        private static readonly object s_Lock = new();

        public static int TypeCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { lock (s_Lock) return s_TypeToId.Count; }
        }

        public static CapabilityTypeId GetTypeId(Type capabilityType)
        {
            if (capabilityType == null) throw new ArgumentNullException(nameof(capabilityType));
            if (!typeof(Capability).IsAssignableFrom(capabilityType))
                throw new ArgumentException($"Type {capabilityType.Name} must inherit from Capability");

            if (s_TypeToId.TryGetValue(capabilityType, out var typeId))
                return typeId;

            lock (s_Lock)
            {
                if (s_TypeToId.TryGetValue(capabilityType, out typeId))
                    return typeId;

                var id = Interlocked.Increment(ref s_NextTypeId) - 1;
                typeId = new CapabilityTypeId(id);

                s_TypeToId[capabilityType] = typeId;
                s_IdToType[id] = capabilityType;

                return typeId;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType(int typeId)
        {
            lock (s_Lock) { return s_IdToType.TryGetValue(typeId, out var type) ? type : null; }
        }
    }

    /// <summary>
    ///   <para>能力类型注册表</para>
    /// </summary>
    /// <typeparam name="T">能力类型</typeparam>
    internal static class CapabilityTypeRegistry<T> where T : Capability
    {
        public static readonly CapabilityTypeId id = CapabilityTypeRegistry.GetTypeId(typeof(T));
    }

    #endregion
    
    #region 标签系统

    /// <summary>
    ///   <para>标签注册表</para>
    /// </summary>
    internal static class TagRegistry
    {
        [ThreadStatic] private static int s_NextTagId = 1;
        private static readonly Dictionary<string, TagId> s_NameToId = new(32);
        private static readonly Dictionary<int, string> s_IdToName = new(32);
        private static readonly object s_Lock = new();

        public static TagId GetTagId(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("Tag name cannot be null or empty");

            if (s_NameToId.TryGetValue(tagName, out var tagId))
                return tagId;

            lock (s_Lock)
            {
                if (s_NameToId.TryGetValue(tagName, out tagId))
                    return tagId;

                var id = Interlocked.Increment(ref s_NextTagId) - 1;
                tagId = new TagId(id);

                s_NameToId[tagName] = tagId;
                s_IdToName[id] = tagName;

                return tagId;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTagName(TagId tagId)
        {
            lock (s_Lock) { return s_IdToName.TryGetValue(tagId, out var name) ? name : null; }
        }

        public static void PreRegister(params string[] tagNames)
        {
            if (tagNames == null || tagNames.Length == 0) return;

            lock (s_Lock)
            {
                foreach (var name in tagNames)
                {
                    if (!string.IsNullOrEmpty(name) && !s_NameToId.ContainsKey(name))
                        GetTagId(name);
                }
            }
        }
    }

    #endregion
    
    #region 组件掩码系统

    /// <summary>
    ///   <para>组件掩码（位图，用于快速组件匹配）</para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentMask : IEquatable<ComponentMask>
    {
        private const int BITS_PER_BLOCK = 64;      // 每个块64位
        private const int INIT_BLOCK_COUNT = 4;     // 初始块数量
        private const int MAX_INLINE_BLOCKS = 4;    // 内联块数量，避免小掩码的堆分配

        private ulong m_InlineBlock0;               // 内联块1
        private ulong m_InlineBlock1;               // 内联块2
        private ulong m_InlineBlock2;               // 内联块3
        private ulong m_InlineBlock3;               // 内联块4
        private ulong[] m_Blocks;                   // 仅在超过内联大小时使用
        private int m_BlockCount;                   // 块数量

        /// <summary>
        ///   <para>掩码是否为空</para>
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_InlineBlock0 != 0 || m_InlineBlock1 != 0 || 
                    m_InlineBlock2 != 0 || m_InlineBlock3 != 0) 
                    return false;
                    
                if (m_Blocks != null)
                {
                    for (int i = MAX_INLINE_BLOCKS; i < m_BlockCount; i++)
                        if (m_Blocks[i] != 0) return false;
                }
                return true;
            }
        }

        public ComponentMask(int initialCapacity = 64)
        {
            int blockCount = Math.Max((initialCapacity + BITS_PER_BLOCK - 1) / BITS_PER_BLOCK, INIT_BLOCK_COUNT);
            m_InlineBlock0 = m_InlineBlock1 = m_InlineBlock2 = m_InlineBlock3 = 0;
            m_Blocks = blockCount > MAX_INLINE_BLOCKS ? new ulong[blockCount] : null;
            m_BlockCount = blockCount;
        }

        /// <summary>
        ///   <para>设置指定组件类型在掩码中的存在状态</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int componentTypeId, bool value)
        {
            if (componentTypeId < 0) return;
            EnsureCapacity(componentTypeId + 1);

            int blockIndex = componentTypeId / BITS_PER_BLOCK;
            int bitIndex = componentTypeId % BITS_PER_BLOCK;
            ulong mask = 1UL << bitIndex;

            ulong block = GetBlock(blockIndex);
            if (value) block |= mask;
            else block &= ~mask;
            SetBlock(blockIndex, block);
        }

        /// <summary>
        ///   <para>获取指定组件类型在掩码中的存在状态</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int componentTypeId)
        {
            if (componentTypeId < 0) return false;

            int blockIndex = componentTypeId / BITS_PER_BLOCK;
            if (blockIndex >= m_BlockCount) return false;

            int bitIndex = componentTypeId % BITS_PER_BLOCK;
            return (GetBlock(blockIndex) & (1UL << bitIndex)) != 0;
        }

        /// <summary>
        ///   <para>添加指定组件类型到掩码</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int componentTypeId) => Set(componentTypeId, true);

        /// <summary>
        ///   <para>从掩码中移除指定组件类型</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int componentTypeId) => Set(componentTypeId, false);

        /// <summary>
        ///   <para>清空掩码中的所有组件标记</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            m_InlineBlock0 = m_InlineBlock1 = m_InlineBlock2 = m_InlineBlock3 = 0;
            if (m_Blocks != null) 
                Array.Clear(m_Blocks, 0, m_BlockCount);
        }

        /// <summary>
        ///   <para>判断当前掩码是否包含另一个掩码的全部组件</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsAll(in ComponentMask other)
        {
            int minBlocks = Math.Min(m_BlockCount, other.m_BlockCount);
            
            for (int i = 0; i < minBlocks; i++)
            {
                ulong otherBlock = other.GetBlock(i);
                if ((GetBlock(i) & otherBlock) != otherBlock)
                    return false;
            }

            for (int i = minBlocks; i < other.m_BlockCount; i++)
                if (other.GetBlock(i) != 0)
                    return false;

            return true;
        }

        /// <summary>
        ///   <para>判断当前掩码与另一个掩码是否存在任意交集</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsAny(in ComponentMask other)
        {
            int minBlocks = Math.Min(m_BlockCount, other.m_BlockCount);
            for (int i = 0; i < minBlocks; i++)
                if ((GetBlock(i) & other.GetBlock(i)) != 0)
                    return true;

            return false;
        }

        /// <summary>
        ///   <para>判断当前掩码与另一个掩码是否完全无交集</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsNone(in ComponentMask other) => !ContainsAny(other);

        /// <summary>
        ///   <para>创建当前掩码的副本</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentMask Clone()
        {
            var clone = new ComponentMask();
            clone.m_InlineBlock0 = m_InlineBlock0;
            clone.m_InlineBlock1 = m_InlineBlock1;
            clone.m_InlineBlock2 = m_InlineBlock2;
            clone.m_InlineBlock3 = m_InlineBlock3;
            clone.m_BlockCount = m_BlockCount;
            
            if (m_Blocks != null)
            {
                clone.m_Blocks = new ulong[m_Blocks.Length];
                Array.Copy(m_Blocks, clone.m_Blocks, m_BlockCount);
            }
            
            return clone;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetBlock(int blockIndex)
        {
            if (blockIndex < MAX_INLINE_BLOCKS)
            {
                switch (blockIndex)
                {
                    case 0: return m_InlineBlock0;
                    case 1: return m_InlineBlock1;
                    case 2: return m_InlineBlock2;
                    case 3: return m_InlineBlock3;
                }
            }
            return m_Blocks[blockIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBlock(int blockIndex, ulong value)
        {
            if (blockIndex < MAX_INLINE_BLOCKS)
            {
                switch (blockIndex)
                {
                    case 0: m_InlineBlock0 = value; break;
                    case 1: m_InlineBlock1 = value; break;
                    case 2: m_InlineBlock2 = value; break;
                    case 3: m_InlineBlock3 = value; break;
                }
            }
            else
            {
                m_Blocks[blockIndex] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int requiredBits)
        {
            int requiredBlocks = (requiredBits + BITS_PER_BLOCK - 1) / BITS_PER_BLOCK;

            if (m_BlockCount < requiredBlocks)
            {
                int newBlockCount = Math.Max(m_BlockCount * 2, requiredBlocks);
                
                if (m_Blocks == null && newBlockCount > MAX_INLINE_BLOCKS)
                {
                    m_Blocks = new ulong[newBlockCount];
                    m_Blocks[0] = m_InlineBlock0;
                    m_Blocks[1] = m_InlineBlock1;
                    m_Blocks[2] = m_InlineBlock2;
                    m_Blocks[3] = m_InlineBlock3;
                }
                else if (m_Blocks != null)
                {
                    Array.Resize(ref m_Blocks, newBlockCount);
                }
                
                m_BlockCount = newBlockCount;
            }
        }

        public bool Equals(ComponentMask other)
        {
            if (m_InlineBlock0 != other.m_InlineBlock0 || m_InlineBlock1 != other.m_InlineBlock1 ||
                m_InlineBlock2 != other.m_InlineBlock2 || m_InlineBlock3 != other.m_InlineBlock3)
                return false;

            int maxBlocks = Math.Max(m_BlockCount, other.m_BlockCount);
            for (int i = MAX_INLINE_BLOCKS; i < maxBlocks; i++)
            {
                ulong left = i < m_BlockCount && m_Blocks != null ? m_Blocks[i] : 0;
                ulong right = i < other.m_BlockCount && other.m_Blocks != null ? other.m_Blocks[i] : 0;
                if (left != right) return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ComponentMask other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + m_InlineBlock0.GetHashCode();
            hash = hash * 31 + m_InlineBlock1.GetHashCode();
            hash = hash * 31 + m_InlineBlock2.GetHashCode();
            hash = hash * 31 + m_InlineBlock3.GetHashCode();

            if (m_Blocks != null)
            {
                for (int i = MAX_INLINE_BLOCKS; i < m_BlockCount; i++)
                    if (m_Blocks[i] != 0)
                        hash = hash * 31 + m_Blocks[i].GetHashCode();
            }

            return hash;
        }

        public Enumerator GetEnumerator() => new(this);

        public ref struct Enumerator
        {
            private readonly ComponentMask m_Mask;
            private int m_BlockIndex;
            private int m_BitIndex;
            private ulong m_CurrentBlock;
            private int m_Current;

            public Enumerator(ComponentMask mask)
            {
                m_Mask = mask;
                m_BlockIndex = -1;
                m_BitIndex = -1;
                m_CurrentBlock = 0;
                m_Current = -1;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (m_BitIndex < 0)
                    {
                        m_BlockIndex++;
                        if (m_BlockIndex >= m_Mask.m_BlockCount) return false;

                        m_CurrentBlock = m_Mask.GetBlock(m_BlockIndex);
                        m_BitIndex = 0;

                        if (m_CurrentBlock == 0)
                        {
                            m_BitIndex = -1;
                            continue;
                        }
                    }

                    while (m_BitIndex < BITS_PER_BLOCK)
                    {
                        if ((m_CurrentBlock & (1UL << m_BitIndex)) != 0)
                        {
                            m_Current = m_BlockIndex * BITS_PER_BLOCK + m_BitIndex;
                            m_BitIndex++;
                            return true;
                        }
                        m_BitIndex++;
                    }

                    m_BitIndex = -1;
                }
            }

            public int Current => m_Current;
        }
    }

    #endregion
    
    #region 组件池系统

    internal interface IComponentPool : IDisposable
    {
        int TypeId { get; }
        int Count { get; }
        int Capacity { get; }
        bool Has(Actor actor);
        bool Remove(Actor actor);
        void Clear();
        void EnsureCapacity(int capacity);
    }

    /// <summary>
    ///   <para>组件池</para>
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    internal sealed class ComponentPool<T> : IComponentPool where T : struct, IComponent
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Entry
        {
            public T value;
            public int actorVersion;
            public bool isActive;
        }

        private Entry[] m_Entries;
        private int[] m_Sparse;
        private int[] m_Dense;
        private int m_Count;
        private int m_Capacity;
        private readonly SpinLock m_Lock;

        public ComponentTypeId TypeId => ComponentTypeRegistry<T>.id;
        int IComponentPool.TypeId => TypeId;
        public int Count => m_Count;
        public int Capacity => m_Capacity;

        public ComponentPool(int initialCapacity = 64)
        {
            m_Capacity = Math.Max(initialCapacity, 16);
            m_Entries = new Entry[m_Capacity];
            m_Sparse = new int[m_Capacity];
            m_Dense = new int[m_Capacity];
            m_Sparse.AsSpan().Fill(-1);
            m_Lock = new SpinLock(false);
        }

        public ref T Add(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                EnsureSparseCapacity(actorIndex + 1);

                if (m_Count >= m_Capacity)
                    Resize(m_Capacity * 2);

                int denseIndex = m_Count++;
                m_Sparse[actorIndex] = denseIndex;
                m_Dense[denseIndex] = actorIndex;

                ref var entry = ref m_Entries[denseIndex];
                entry.value = default;
                entry.actorVersion = actor.Version;
                entry.isActive = true;

                return ref entry.value;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        public ref T Get(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length)
                    ThrowActorNotFound(actor);

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count)
                    ThrowComponentNotFound(actor);

                ref var entry = ref m_Entries[denseIndex];
                if (!entry.isActive || entry.actorVersion != actor.Version)
                    ThrowComponentInvalid(actor);

                return ref entry.value;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        public bool TryGet(Actor actor, out T component)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                component = default;
                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length) return false;

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count) return false;

                ref var entry = ref m_Entries[denseIndex];
                if (!entry.isActive || entry.actorVersion != actor.Version) return false;

                component = entry.value;
                return true;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length) return false;

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count) return false;

                ref var entry = ref m_Entries[denseIndex];
                return entry.isActive && entry.actorVersion == actor.Version;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        public bool Remove(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length) return false;

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count) return false;

                ref var entry = ref m_Entries[denseIndex];
                if (!entry.isActive || entry.actorVersion != actor.Version) return false;

                int lastIndex = --m_Count;
                if (denseIndex != lastIndex)
                {
                    m_Entries[denseIndex] = m_Entries[lastIndex];
                    int movedActorIndex = m_Dense[lastIndex];
                    m_Dense[denseIndex] = movedActorIndex;
                    m_Sparse[movedActorIndex] = denseIndex;
                }

                m_Sparse[actorIndex] = -1;
                m_Entries[lastIndex] = default;

                return true;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        public void Set(Actor actor, in T component)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                int actorIndex = actor.Index;
                if (actorIndex < 0 || actorIndex >= m_Sparse.Length)
                    ThrowActorNotFound(actor);

                int denseIndex = m_Sparse[actorIndex];
                if (denseIndex < 0 || denseIndex >= m_Count)
                    ThrowComponentNotFound(actor);

                ref var entry = ref m_Entries[denseIndex];
                if (!entry.isActive || entry.actorVersion != actor.Version)
                    ThrowComponentInvalid(actor);

                entry.value = component;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        public void Clear()
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                if (m_Count > 0)
                {
                    for (int i = 0; i < m_Count; i++)
                    {
                        int actorIndex = m_Dense[i];
                        if (actorIndex >= 0 && actorIndex < m_Sparse.Length)
                            m_Sparse[actorIndex] = -1;
                    }

                    Array.Clear(m_Entries, 0, m_Count);
                    Array.Clear(m_Dense, 0, m_Count);

                    m_Count = 0;
                }
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        public void EnsureCapacity(int capacity)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                if (capacity > m_Capacity) Resize(capacity);
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        public void Dispose()
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                m_Entries = null;
                m_Sparse = null;
                m_Dense = null;
                m_Count = 0;
                m_Capacity = 0;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        internal delegate void ComponentAction<TContext>(ref TContext context, Actor actor, ref T component);

        internal void ForEach<TContext>(ref TContext context, ComponentAction<TContext> action)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                for (int i = 0; i < m_Count; i++)
                {
                    ref var entry = ref m_Entries[i];
                    if (!entry.isActive) continue;
                    int actorIndex = m_Dense[i];
                    var actor = new Actor(actorIndex, entry.actorVersion);
                    action(ref context, actor, ref entry.value);
                }
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        private void Resize(int newCapacity)
        {
            newCapacity = Math.Max(newCapacity, 16);
            if (newCapacity == m_Capacity) return;

            Array.Resize(ref m_Entries, newCapacity);
            Array.Resize(ref m_Dense, newCapacity);
            m_Capacity = newCapacity;
        }

        private void EnsureSparseCapacity(int requiredCapacity)
        {
            if (requiredCapacity <= m_Sparse.Length) return;

            int oldLength = m_Sparse.Length;
            int newCapacity = Math.Max(oldLength * 2, requiredCapacity);
            Array.Resize(ref m_Sparse, newCapacity);

            for (int i = oldLength; i < newCapacity; i++)
                m_Sparse[i] = -1;
        }

        private static void ThrowActorNotFound(Actor actor) =>
            throw new InvalidOperationException($"Actor {actor} not found");
        private static void ThrowComponentNotFound(Actor actor) =>
            throw new InvalidOperationException($"Component not found for actor {actor}");
        private static void ThrowComponentInvalid(Actor actor) =>
            throw new InvalidOperationException($"Component not valid for actor {actor}");
    }

    #endregion
    
    #region 行动者管理系统

    /// <summary>
    ///   <para>行动者数据</para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ActorData
    {
        public int version;
        public bool isAlive;
        public ComponentMask componentMask;
        public List<CapabilityInstance> capabilities;
        public TagBlockMask tagBlocks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            isAlive = false;
            componentMask.Clear();
            capabilities?.Clear();
            tagBlocks.Clear();
        }
    }

    /// <summary>
    ///   <para>标签阻塞掩码（记录哪些标签被哪些能力阻塞）</para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TagBlockMask
    {
        private const int INITIAL_CAPACITY = 4;

        private struct BlockEntry
        {
            public object instigator;
            public int blockCount;
        }

        private Dictionary<int, List<BlockEntry>> m_TagBlocks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTagBlocked(TagId tagId)
        {
            if (m_TagBlocks == null) return false;
            
            if (m_TagBlocks.TryGetValue(tagId, out var blocks))
            {
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].blockCount > 0)
                        return true;
                }
            }
            
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BlockTag(TagId tagId, object instigator)
        {
            m_TagBlocks ??= new Dictionary<int, List<BlockEntry>>(INITIAL_CAPACITY);

            if (!m_TagBlocks.TryGetValue(tagId, out var blocks))
            {
                blocks = new List<BlockEntry>(2);
                m_TagBlocks[tagId] = blocks;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                var entry = blocks[i];
                if (entry.instigator == instigator)
                {
                    entry.blockCount++;
                    blocks[i] = entry;
                    return;
                }
            }

            blocks.Add(new BlockEntry { instigator = instigator, blockCount = 1 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnblockTag(TagId tagId, object instigator)
        {
            if (m_TagBlocks == null || !m_TagBlocks.TryGetValue(tagId, out var blocks))
                return;

            for (int i = 0; i < blocks.Count; i++)
            {
                var entry = blocks[i];
                if (entry.instigator == instigator)
                {
                    entry.blockCount--;
                    if (entry.blockCount <= 0)
                    {
                        blocks.RemoveAt(i);
                        if (blocks.Count == 0)
                            m_TagBlocks.Remove(tagId);
                    }
                    else
                    {
                        blocks[i] = entry;
                    }
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (m_TagBlocks != null)
            {
                m_TagBlocks.Clear();
                m_TagBlocks = null;
            }
        }
    }

    /// <summary>
    ///   <para>行为管理器配置项</para>
    /// </summary>
    [Serializable]
    public readonly struct ActorManagerOptions
    {
        public const int MIN_CAPACITY = 64;                 // 最小行为者容量
        public const int MAX_CAPACITY = 2 * 1024 * 1024;    // 最大行为者容量

        /// <summary>
        ///   <para>初始容量（受最小/最大容量限制）</para>
        /// </summary>
        public readonly int initialCapacity;
        /// <summary>
        ///   <para>是否启用多线程</para>
        /// </summary>
        public readonly bool enableMultiThreading;
        /// <summary>
        ///   <para>容量增长因子（至少为 1.0）</para>
        /// </summary>
        public readonly float growthFactor;

        /// <summary>
        ///   <para>构造配置项</para>
        /// </summary>
        /// <param name="initialCapacity">初始容量</param>
        /// <param name="enableMultiThreading">是否启用多线程</param>
        /// <param name="growthFactor">容量增长因子</param>
        public ActorManagerOptions(
            int initialCapacity,
            bool enableMultiThreading,
            float growthFactor)
        {
            this.initialCapacity = Math.Clamp(initialCapacity, MIN_CAPACITY, MAX_CAPACITY);
            this.enableMultiThreading = enableMultiThreading;
            this.growthFactor = Math.Max(growthFactor, 1.0f);
        }

        /// <summary>
        ///   <para>默认配置（1024容量，启用并发，增长因子 2）</para>
        /// </summary>
        public static ActorManagerOptions Default => new(
            initialCapacity: 1024,
            enableMultiThreading: true,
            growthFactor: 2.0f);
    }

    /// <summary>
    ///   <para>行动者管理器</para>
    /// </summary>
    [Serializable]
    public sealed class ActorManager : IDisposable
    {
        private int m_Capacity;
        private int m_FreeCount;
        private int[] m_FreeList;
        private float m_GrowthFactor;
        private ActorData[] m_Actors;

        private readonly SpinLock m_PoolLock;
        private readonly ReaderWriterLockSlim m_ActorLock;
        private readonly Dictionary<int, IComponentPool> m_ComponentPools;
        private readonly bool m_EnableMultiThreading;

        private bool m_IsDisposed;

        /// <summary>
        ///   <para>当前容量（可容纳的最大行动者数量）</para>
        /// </summary>
        public int Capacity => m_Capacity;
        
        /// <summary>
        ///   <para>存活行动者数量</para>
        /// </summary>
        public int AliveActorCount { get; private set; }
        
        /// <summary>
        ///   <para>空闲槽位数量</para>
        /// </summary>
        public int FreeActorCount => m_FreeCount;
        
        /// <summary>
        ///   <para>是否启用多线程</para>
        /// </summary>
        public bool EnableMultiThreading => m_EnableMultiThreading;

        internal ActorManager(ActorManagerOptions options)
        {
            m_Capacity = options.initialCapacity;
            m_GrowthFactor = options.growthFactor;
            m_EnableMultiThreading = options.enableMultiThreading;

            m_Actors = new ActorData[m_Capacity];
            m_FreeList = new int[m_Capacity];

            m_ActorLock = m_EnableMultiThreading
                ? new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion)
                : null;

            m_PoolLock = new SpinLock(false);
            m_ComponentPools = new Dictionary<int, IComponentPool>(128);

            for (int i = 0; i < m_Capacity; i++)
                m_FreeList[i] = i;

            m_FreeCount = m_Capacity;
        }
        
        ~ActorManager() => Dispose();

        public Actor CreateActor()
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (m_FreeCount == 0)
                {
                    int newCapacity = (int)(m_Capacity * m_GrowthFactor);
                    Resize(Math.Min(newCapacity, ActorManagerOptions.MAX_CAPACITY));
                    if (m_FreeCount == 0) ThrowCapacityExceeded();
                }

                int index = m_FreeList[--m_FreeCount];
                ref var actorData = ref m_Actors[index];

                actorData.version = actorData.version == 0 ? 1 : actorData.version + 1;
                actorData.isAlive = true;
                actorData.componentMask = new ComponentMask(64);

                var actor = new Actor(index, actorData.version);
                AliveActorCount++;

                return actor;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        public Actor[] CreateActors(int count)
        {
            if (count <= 0) return Array.Empty<Actor>();

            var actors = new Actor[count];

            m_ActorLock?.EnterWriteLock();
            try
            {
                if (m_FreeCount < count)
                {
                    int newCapacity = Math.Min(
                        Math.Max((int)(m_Capacity * m_GrowthFactor), m_Capacity + count),
                        ActorManagerOptions.MAX_CAPACITY);
                    Resize(newCapacity);
                }

                for (int i = 0; i < count && m_FreeCount > 0; i++)
                {
                    int index = m_FreeList[--m_FreeCount];
                    ref var actorData = ref m_Actors[index];

                    actorData.version = actorData.version == 0 ? 1 : actorData.version + 1;
                    actorData.isAlive = true;
                    actorData.componentMask = new ComponentMask(64);

                    actors[i] = new Actor(index, actorData.version);
                    AliveActorCount++;
                }
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }

            return actors;
        }

        public void DestroyActor(Actor actor)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return;

                ref var actorData = ref m_Actors[actor.Index];

                foreach (var typeId in actorData.componentMask)
                    RemoveComponentByTypeIdUnsafe(actor, typeId);

                if (actorData.capabilities != null)
                {
                    for (int i = 0; i < actorData.capabilities.Count; i++)
                    {
                        var instance = actorData.capabilities[i];
                        instance.capability?.Dispose();
                    }
                    actorData.capabilities.Clear();
                }

                actorData.Reset();
                m_FreeList[m_FreeCount++] = actor.Index;
                AliveActorCount--;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        public bool HasActor(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                return IsActorAliveUnsafe(actor);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActorAlive(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try { return IsActorAliveUnsafe(actor); }
            finally { m_ActorLock?.ExitReadLock(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Actor actor) where T : struct, IComponent
        {
            return HasComponent(actor, ComponentTypeRegistry<T>.id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(Actor actor, int componentTypeId)
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                return IsActorAliveUnsafe(actor) &&
                       m_Actors[actor.Index].componentMask.Get(componentTypeId);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentMask GetComponentMask(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try { return IsActorAliveUnsafe(actor) ? m_Actors[actor.Index].componentMask : default; }
            finally { m_ActorLock?.ExitReadLock(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TagBlockMask GetTagBlockMask(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try { return IsActorAliveUnsafe(actor) ? m_Actors[actor.Index].tagBlocks : default; }
            finally { m_ActorLock?.ExitReadLock(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal List<CapabilityInstance> GetCapabilities(Actor actor)
        {
            m_ActorLock?.EnterReadLock();
            try { return IsActorAliveUnsafe(actor) ? m_Actors[actor.Index].capabilities : null; }
            finally { m_ActorLock?.ExitReadLock(); }
        }

        internal void AddCapabilityInstance(Actor actor, CapabilityInstance instance)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return;

                ref var actorData = ref m_Actors[actor.Index];
                actorData.capabilities ??= new List<CapabilityInstance>(16);
                actorData.capabilities.Add(instance);
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        internal bool RemoveCapabilityInstance(Actor actor, Capability capability)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return false;

                ref var actorData = ref m_Actors[actor.Index];
                if (actorData.capabilities == null) return false;

                for (int i = 0; i < actorData.capabilities.Count; i++)
                {
                    if (actorData.capabilities[i].capability == capability)
                    {
                        actorData.capabilities.RemoveAt(i);
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        internal void BlockTag(Actor actor, TagId tagId, object instigator)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return;
                ref var actorData = ref m_Actors[actor.Index];
                actorData.tagBlocks.BlockTag(tagId, instigator);
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        internal void UnblockTag(Actor actor, TagId tagId, object instigator)
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return;
                ref var actorData = ref m_Actors[actor.Index];
                actorData.tagBlocks.UnblockTag(tagId, instigator);
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        internal bool IsTagBlocked(Actor actor, TagId tagId)
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return false;
                ref var actorData = ref m_Actors[actor.Index];
                return actorData.tagBlocks.IsTagBlocked(tagId);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        public ref T AddComponent<T>(Actor actor) where T : struct, IComponent
        {
            var typeId = ComponentTypeRegistry<T>.id;

            m_ActorLock?.EnterUpgradeableReadLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) ThrowActorNotAlive(actor);

                var pool = GetOrCreatePool<T>();

                m_ActorLock?.EnterWriteLock();
                try
                {
                    ref var component = ref pool.Add(actor);
                    ref var actorData = ref m_Actors[actor.Index];
                    actorData.componentMask.Add(typeId);

                    return ref component;
                }
                finally
                {
                    m_ActorLock?.ExitWriteLock();
                }
            }
            finally
            {
                m_ActorLock?.ExitUpgradeableReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(Actor actor) where T : struct, IComponent
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) ThrowActorNotAlive(actor);

                var pool = GetPool<T>();
                if (pool == null) ThrowComponentNotFound<T>(actor);

                return ref pool.Get(actor);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(Actor actor, out T component) where T : struct, IComponent
        {
            m_ActorLock?.EnterReadLock();
            try
            {
                component = default;
                if (!IsActorAliveUnsafe(actor)) return false;

                var pool = GetPool<T>();
                return pool != null && pool.TryGet(actor, out component);
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent<T>(Actor actor) where T : struct, IComponent
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) return false;
                return RemoveComponentByTypeIdUnsafe(actor, ComponentTypeRegistry<T>.id);
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        public void SetComponent<T>(Actor actor, in T component) where T : struct, IComponent
        {
            var typeId = ComponentTypeRegistry<T>.id;

            m_ActorLock?.EnterUpgradeableReadLock();
            try
            {
                if (!IsActorAliveUnsafe(actor)) ThrowActorNotAlive(actor);

                var pool = GetOrCreatePool<T>();

                if (pool.Has(actor))
                {
                    pool.Set(actor, component);
                }
                else
                {
                    m_ActorLock?.EnterWriteLock();
                    try
                    {
                        ref var comp = ref pool.Add(actor);
                        comp = component;

                        ref var actorData = ref m_Actors[actor.Index];
                        actorData.componentMask.Add(typeId);
                    }
                    finally
                    {
                        m_ActorLock?.ExitWriteLock();
                    }
                }
            }
            finally
            {
                m_ActorLock?.ExitUpgradeableReadLock();
            }
        }

        public IReadOnlyList<Actor> QueryActorsWithComponent<T>() where T : struct, IComponent
        {
            var result = new List<Actor>();

            m_ActorLock?.EnterReadLock();
            try
            {
                var typeId = ComponentTypeRegistry<T>.id;

                for (int i = 0; i < m_Capacity; i++)
                {
                    ref var actorData = ref m_Actors[i];
                    if (!actorData.isAlive) continue;

                    if (actorData.componentMask.Get(typeId))
                    {
                        result.Add(new Actor(i, actorData.version));
                    }
                }
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }

            return result;
        }

        public IReadOnlyList<Actor> QueryActorsWithComponents(params Type[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0)
                return new List<Actor>();

            var requiredMask = new ComponentMask(componentTypes.Length * 2);
            foreach (var type in componentTypes)
            {
                if (type == null) continue;
                if (!typeof(IComponent).IsAssignableFrom(type))
                    throw new ArgumentException($"Type {type.Name} must implement IComponent");
                if (!type.IsValueType)
                    throw new ArgumentException($"Component {type.Name} must be a struct");

                requiredMask.Add(ComponentTypeRegistry.GetTypeId(type));
            }

            var result = new List<Actor>();

            m_ActorLock?.EnterReadLock();
            try
            {
                for (int i = 0; i < m_Capacity; i++)
                {
                    ref var actorData = ref m_Actors[i];
                    if (!actorData.isAlive) continue;

                    if (actorData.componentMask.ContainsAll(requiredMask))
                    {
                        result.Add(new Actor(i, actorData.version));
                    }
                }
            }
            finally
            {
                m_ActorLock?.ExitReadLock();
            }

            return result;
        }

        public void Clear()
        {
            m_ActorLock?.EnterWriteLock();
            try
            {
                bool poolLockTaken = false;
                try
                {
                    m_PoolLock.Enter(ref poolLockTaken);
                    foreach (var pool in m_ComponentPools.Values)
                        pool.Clear();
                }
                finally
                {
                    if (poolLockTaken) m_PoolLock.Exit();
                }

                for (int i = 0; i < m_Capacity; i++)
                    m_Actors[i] = default;

                for (int i = 0; i < m_Capacity; i++)
                    m_FreeList[i] = i;

                m_FreeCount = m_Capacity;
                AliveActorCount = 0;
            }
            finally
            {
                m_ActorLock?.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;

            Clear();

            m_ActorLock?.EnterWriteLock();
            try
            {
                bool lockTaken = false;
                try
                {
                    m_PoolLock.Enter(ref lockTaken);
                    foreach (var pool in m_ComponentPools.Values)
                        pool.Dispose();
                    m_ComponentPools.Clear();
                }
                finally
                {
                    if (lockTaken) m_PoolLock.Exit();
                }
            }
            finally
            {
                if (m_ActorLock != null)
                {
                    m_ActorLock.ExitWriteLock();
                    m_ActorLock.Dispose();
                }
            }

            m_IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsActorAliveUnsafe(Actor actor)
        {
            if (actor.Index < 0 || actor.Index >= m_Capacity) return false;
            ref var actorData = ref m_Actors[actor.Index];
            return actorData.isAlive && actorData.version == actor.Version;
        }

        private bool RemoveComponentByTypeIdUnsafe(Actor actor, int typeId)
        {
            bool lockTaken = false;
            try
            {
                m_PoolLock.Enter(ref lockTaken);
                if (!m_ComponentPools.TryGetValue(typeId, out var pool)) return false;
                if (!pool.Remove(actor)) return false;
            }
            finally
            {
                if (lockTaken) m_PoolLock.Exit();
            }

            ref var actorData = ref m_Actors[actor.Index];
            actorData.componentMask.Remove(typeId);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ComponentPool<T> GetOrCreatePool<T>() where T : struct, IComponent
        {
            var typeId = ComponentTypeRegistry<T>.id;

            bool lockTaken = false;
            try
            {
                m_PoolLock.Enter(ref lockTaken);
                if (!m_ComponentPools.TryGetValue(typeId, out var poolObj))
                {
                    var pool = new ComponentPool<T>(Math.Max(AliveActorCount / 4, 64));
                    m_ComponentPools[typeId] = pool;
                    return pool;
                }

                return (ComponentPool<T>)poolObj;
            }
            finally
            {
                if (lockTaken) m_PoolLock.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentPool<T> GetPool<T>() where T : struct, IComponent
        {
            var typeId = ComponentTypeRegistry<T>.id;

            bool lockTaken = false;
            try
            {
                m_PoolLock.Enter(ref lockTaken);
                return m_ComponentPools.TryGetValue(typeId, out var poolObj)
                    ? (ComponentPool<T>)poolObj
                    : null;
            }
            finally
            {
                if (lockTaken) m_PoolLock.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int newCapacity)
        {
            if (newCapacity <= m_Capacity || newCapacity > ActorManagerOptions.MAX_CAPACITY) return;

            int oldCapacity = m_Capacity;
            m_Capacity = newCapacity;

            Array.Resize(ref m_Actors, newCapacity);
            Array.Resize(ref m_FreeList, newCapacity);

            for (int i = 0; i < oldCapacity; i++)
                m_Actors[i].componentMask = m_Actors[i].componentMask.Clone();

            for (int i = oldCapacity; i < newCapacity; i++)
                m_FreeList[m_FreeCount++] = i;
        }

        private static void ThrowCapacityExceeded() =>
            throw new InvalidOperationException("Actor capacity exceeded");
        private static void ThrowActorNotAlive(Actor actor) =>
            throw new InvalidOperationException($"Actor {actor} is not alive");
        private static void ThrowComponentNotFound<T>(Actor actor) =>
            throw new InvalidOperationException($"Component {typeof(T).Name} not found for actor {actor}");
    }

    #endregion
    
    #region 能力更新系统

    /// <summary>
    ///   <para>能力更新分组（按TickGroup组织）</para>
    /// </summary>
    internal sealed class CapabilityTickGroup
    {
        public readonly int groupId;
        public readonly List<Capability> activeCapabilities;
        public readonly List<Capability> inactiveCapabilities;
        
        public CapabilityTickGroup(int groupId)
        {
            this.groupId = groupId;
            activeCapabilities = new List<Capability>(64);
            inactiveCapabilities = new List<Capability>(64);
        }

        public void Clear()
        {
            activeCapabilities?.Clear();
            inactiveCapabilities?.Clear();
        }
    }

    /// <summary>
    ///   <para>动作队列</para>
    /// </summary>
    [Serializable]
    internal sealed class ActionQueue
    {
        private const int PARALLEL_MIN_COUNT = 16;
        
        private readonly List<CapabilityTickGroup> m_Groups;
        private readonly Dictionary<int, CapabilityTickGroup> m_GroupMap;
        private readonly Dictionary<Capability, (int groupIndex, int listIndex, bool isActive)> m_CapabilityIndices;
        private readonly HashSet<Capability> m_DirtyCapabilities;
        private bool m_IsSorted;
        
        public ActionQueue()
        {
            m_Groups = new List<CapabilityTickGroup>(16);
            m_GroupMap = new Dictionary<int, CapabilityTickGroup>(16);
            m_CapabilityIndices = new Dictionary<Capability, (int, int, bool)>(256);
            m_DirtyCapabilities = new HashSet<Capability>(256);

            foreach (var group in Enum.GetValues(typeof(TickGroup)))
            {
                var gid = (int)group;
                var updateGroup = new CapabilityTickGroup(gid);
                m_Groups.Add(updateGroup);
                m_GroupMap[gid] = updateGroup;
            }
        }
        
        public void AddCapability(Capability capability, int tickGroup, int tickOrder)
        {
            if (!m_GroupMap.TryGetValue(tickGroup, out var group))
            {
                group = new CapabilityTickGroup(tickGroup);
                m_Groups.Add(group);
                m_GroupMap[tickGroup] = group;
                m_IsSorted = false;
            }
            
            int listIndex = group.inactiveCapabilities.Count;
            group.inactiveCapabilities.Add(capability);
            
            m_CapabilityIndices[capability] = (m_Groups.IndexOf(group), listIndex, false);
            m_DirtyCapabilities.Add(capability);
        }
        
        public bool RemoveCapability(Capability capability)
        {
            if (!m_CapabilityIndices.TryGetValue(capability, out var indices))
                return false;
            
            var group = m_Groups[indices.groupIndex];
            var list = indices.isActive ? group.activeCapabilities : group.inactiveCapabilities;
            
            int listIndex = indices.listIndex;
            int lastIndex = list.Count - 1;
            
            if (listIndex != lastIndex)
            {
                var lastCapability = list[lastIndex];
                list[listIndex] = lastCapability;
                m_CapabilityIndices[lastCapability] = (indices.groupIndex, listIndex, indices.isActive);
            }
            
            list.RemoveAt(lastIndex);
            m_CapabilityIndices.Remove(capability);
            m_DirtyCapabilities.Remove(capability);
            
            return true;
        }
        
        public void MarkCapabilityForActivationCheck(Capability capability)
        {
            m_DirtyCapabilities.Add(capability);
        }

        public void Update(float deltaTime, Func<Capability, bool> shouldActivate,
            Func<Capability, bool> shouldDeactivate, bool enableParallel)
        {
            EnsureSorted();
            
            ProcessDirtyCapabilities(shouldActivate, shouldDeactivate);

            for (int i = 0; i < m_Groups.Count; i++)
            {
                var activeList = m_Groups[i].activeCapabilities;
                if (enableParallel)
                    TickCapabilitiesParallel(activeList, deltaTime);
                else
                    TickCapabilitiesSequential(activeList, deltaTime);
            }
        }

        public void Update(float deltaTime, Func<Capability, bool> shouldActivate, Func<Capability, bool> shouldDeactivate, int targetTickGroup, bool enableParallel)
        {
            EnsureSorted();
            
            ProcessDirtyCapabilities(shouldActivate, shouldDeactivate);

            if (m_GroupMap.TryGetValue(targetTickGroup, out var group))
            {
                var activeList = group.activeCapabilities;
                if (enableParallel)
                    TickCapabilitiesParallel(activeList, deltaTime);
                else
                    TickCapabilitiesSequential(activeList, deltaTime);
            }
        }
        
        private void ProcessDirtyCapabilities(Func<Capability, bool> shouldActivate, Func<Capability, bool> shouldDeactivate)
        {
            if (m_DirtyCapabilities.Count == 0) return;
            
            var dirtyArray = ArrayPool<Capability>.Shared.Rent(m_DirtyCapabilities.Count);
            int dirtyCount = 0;
            
            foreach (var capability in m_DirtyCapabilities)
            {
                dirtyArray[dirtyCount++] = capability;
            }
            
            m_DirtyCapabilities.Clear();
            
            try
            {
                for (int i = 0; i < dirtyCount; i++)
                {
                    var capability = dirtyArray[i];
                    
                    if (!m_CapabilityIndices.TryGetValue(capability, out var indices))
                        continue;
                    
                    bool wasActive = indices.isActive;
                    
                    if (!wasActive)
                    {
                        if (shouldActivate(capability))
                        {
                            ActivateCapability(capability, indices);
                        }
                    }
                    else
                    {
                        if (shouldDeactivate(capability))
                        {
                            DeactivateCapability(capability, indices);
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<Capability>.Shared.Return(dirtyArray, false);
            }
        }
        
        private void ActivateCapability(Capability capability, (int groupIndex, int listIndex, bool isActive) indices)
        {
            var group = m_Groups[indices.groupIndex];
            
            int inactiveIndex = indices.listIndex;
            int lastInactiveIndex = group.inactiveCapabilities.Count - 1;
            
            if (inactiveIndex != lastInactiveIndex)
            {
                var lastInactiveCapability = group.inactiveCapabilities[lastInactiveIndex];
                group.inactiveCapabilities[inactiveIndex] = lastInactiveCapability;
                m_CapabilityIndices[lastInactiveCapability] = (indices.groupIndex, inactiveIndex, false);
            }
            
            group.inactiveCapabilities.RemoveAt(lastInactiveIndex);
            
            int insertIndex = FindInsertIndexByTickOrder(group.activeCapabilities, capability.TickOrder);
            group.activeCapabilities.Insert(insertIndex, capability);
            m_CapabilityIndices[capability] = (indices.groupIndex, insertIndex, true);
            for (int i = insertIndex + 1; i < group.activeCapabilities.Count; i++)
            {
                var c = group.activeCapabilities[i];
                m_CapabilityIndices[c] = (indices.groupIndex, i, true);
            }
            
            try
            {
                capability.OnActivated();
                capability.IsActive = true;
            }
            catch (Exception ex)
            {
                HandleCapabilityException(capability, ex);
            }
        }
        
        private void DeactivateCapability(Capability capability, (int groupIndex, int listIndex, bool isActive) indices)
        {
            var group = m_Groups[indices.groupIndex];
            
            int activeIndex = indices.listIndex;
            int lastActiveIndex = group.activeCapabilities.Count - 1;
            
            if (activeIndex != lastActiveIndex)
            {
                var lastActiveCapability = group.activeCapabilities[lastActiveIndex];
                group.activeCapabilities[activeIndex] = lastActiveCapability;
                m_CapabilityIndices[lastActiveCapability] = (indices.groupIndex, activeIndex, true);
            }
            
            group.activeCapabilities.RemoveAt(lastActiveIndex);
            
            int inactiveIndex = group.inactiveCapabilities.Count;
            group.inactiveCapabilities.Add(capability);
            m_CapabilityIndices[capability] = (indices.groupIndex, inactiveIndex, false);
            
            try
            {
                capability.OnDeactivated();
                capability.IsActive = false;
            }
            catch (Exception ex)
            {
                HandleCapabilityException(capability, ex);
            }
        }
        
        public void Clear()
        {
            foreach (var group in m_Groups)
                group.Clear();
            
            m_CapabilityIndices.Clear();
            m_DirtyCapabilities.Clear();
        }
        
        private void EnsureSorted()
        {
            if (m_IsSorted) return;
            
            m_Groups.Sort((a, b) => a.groupId.CompareTo(b.groupId));
            
            m_CapabilityIndices.Clear();
            for (int g = 0; g < m_Groups.Count; g++)
            {
                var group = m_Groups[g];
                
                for (int i = 0; i < group.activeCapabilities.Count; i++)
                {
                    m_CapabilityIndices[group.activeCapabilities[i]] = (g, i, true);
                }
                
                for (int i = 0; i < group.inactiveCapabilities.Count; i++)
                {
                    m_CapabilityIndices[group.inactiveCapabilities[i]] = (g, i, false);
                }
            }
            
            m_IsSorted = true;
        }
        
        private static int FindInsertIndexByTickOrder(List<Capability> list, int order)
        {
            int lo = 0, hi = list.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (list[mid].TickOrder <= order) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
        
        private void TickCapabilitiesSequential(List<Capability> activeList, float deltaTime)
        {
            for (int j = 0; j < activeList.Count; j++)
            {
                var capability = activeList[j];
                TickCapability(capability, deltaTime);
            }
        }
        
        private void TickCapabilitiesParallel(List<Capability> activeList, float deltaTime)
        {
            if (activeList.Count < PARALLEL_MIN_COUNT)
            {
                TickCapabilitiesSequential(activeList, deltaTime);
                return;
            }
            
            int index = 0;
            int count = activeList.Count;
            
            while (index < count)
            {
                var capability = activeList[index];
                if (capability == null || capability.IsDisposed)
                {
                    TickCapability(capability, deltaTime);
                    index++;
                    continue;
                }
                
                int tickOrder = capability.TickOrder;
                int start = index;
                int end = index + 1;
                
                while (end < count)
                {
                    var next = activeList[end];
                    if (next == null || next.IsDisposed || next.TickOrder != tickOrder)
                        break;
                    end++;
                }
                
                int segmentLength = end - start;
                if (segmentLength <= 1)
                {
                    TickCapability(capability, deltaTime);
                }
                else
                {
                    Parallel.For(start, end, i =>
                    {
                        var c = activeList[i];
                        TickCapability(c, deltaTime);
                    });
                }
                
                index = end;
            }
        }
        
        private void TickCapability(Capability capability, float deltaTime)
        {
            if (capability == null || capability.IsDisposed) return;
            
            try
            {
                capability.TickActive(deltaTime);
            }
            catch (Exception ex)
            {
                HandleCapabilityException(capability, ex);
            }
        }
        
        private void HandleCapabilityException(Capability capability, Exception ex)
        {
            capability.IsActive = false;
#if DEBUG
            // Game.LogError($"Capability {capability.GetType().Name} error: {ex}");
            throw new Exception($"Capability {capability.GetType().Name} error: {ex}");
#endif
        }
    }

    #endregion
    
    #region 表单系统
    
    /// <summary>
    ///   <para>能力表单应用模式</para>
    /// </summary>
    public enum CapabilitySheetApplyMode : byte
    {
        /// <summary>
        ///   <para>应用全部</para>
        /// </summary>
        All = 0,
        /// <summary>
        ///   <para>仅组件</para>
        /// </summary>
        ComponentsOnly = 1,
    }
    
    /// <summary>
    ///   <para>能力表单</para>
    /// </summary>
    [Serializable]
    public sealed class CapabilitySheet
    {
        private static readonly Dictionary<Type, MethodInfo> s_AddComponentMethods = new(64);
        private static readonly Dictionary<Type, MethodInfo> s_SetComponentMethods = new(64);
        private static readonly Dictionary<Type, MethodInfo> s_AddCapabilityMethods = new(64);
        private static readonly Dictionary<Type, MethodInfo> s_RemoveCapabilityMethods = new(64);
        private static readonly object s_MethodCacheLock = new();

        private readonly List<Type> m_CapabilityTypes = new(16);
        private readonly List<Type> m_ComponentTypes = new(16);
        private readonly List<NetworkSyncDirection> m_ComponentDirections = new(16);
        private readonly List<CapabilitySheet> m_SubSheets = new(8);
        
        public IReadOnlyList<Type> CapabilityTypes => m_CapabilityTypes;
        public IReadOnlyList<Type> ComponentTypes => m_ComponentTypes;
        public IReadOnlyList<NetworkSyncDirection> ComponentDirections => m_ComponentDirections;
        public IReadOnlyList<CapabilitySheet> SubSheets => m_SubSheets;
        
        public CapabilitySheet AddCapability<T>()
            where T : Capability
        {
            m_CapabilityTypes.Add(typeof(T));
            return this;
        }
        
        public CapabilitySheet AddCapability(Type capabilityType)
        {
            if (capabilityType == null) throw new ArgumentNullException(nameof(capabilityType));
            if (!typeof(Capability).IsAssignableFrom(capabilityType))
                throw new ArgumentException($"Type {capabilityType.Name} must inherit from Capability");
            
            m_CapabilityTypes.Add(capabilityType);
            return this;
        }
        
        public CapabilitySheet AddComponent<T>(NetworkSyncDirection direction = NetworkSyncDirection.None)
            where T : struct, IComponent
        {
            m_ComponentTypes.Add(typeof(T));
            m_ComponentDirections.Add(direction);
            return this;
        }
        
        public CapabilitySheet AddComponent(Type componentType, NetworkSyncDirection direction = NetworkSyncDirection.None)
        {
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));
            if (!typeof(IComponent).IsAssignableFrom(componentType))
                throw new ArgumentException($"Type {componentType.Name} must implement IComponent");
            if (!componentType.IsValueType)
                throw new ArgumentException($"Component {componentType.Name} must be a struct");
            
            m_ComponentTypes.Add(componentType);
            m_ComponentDirections.Add(direction);
            return this;
        }
        
        public CapabilitySheet AddSubSheet(CapabilitySheet subSheet)
        {
            if (subSheet == null) throw new ArgumentNullException(nameof(subSheet));
            m_SubSheets.Add(subSheet);
            return this;
        }
        
        public SheetInstance ApplyTo(Actor actor, World world, CapabilitySheetApplyMode mode = CapabilitySheetApplyMode.All)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            
            var instance = new SheetInstance(actor, world);

            for (int i = 0; i < m_SubSheets.Count; i++)
            {
                instance.AddSubInstance(m_SubSheets[i].ApplyTo(actor, world, mode));
            }
            
            for (int i = 0; i < m_ComponentTypes.Count; i++)
            {
                var componentType = m_ComponentTypes[i];
                var direction = i < m_ComponentDirections.Count
                    ? m_ComponentDirections[i]
                    : NetworkSyncDirection.None;
                AddComponentGeneric(componentType, actor, world, direction);
            }
            
            if (mode == CapabilitySheetApplyMode.All)
            {
                foreach (var capabilityType in m_CapabilityTypes)
                {
                    AddCapabilityToActor(capabilityType, actor, world, instance);
                }
            }
            
            return instance;
        }
        
        public static void RemoveFrom(SheetInstance instance)
        {
            instance?.RemoveFromActor();
        }
        
        private static void AddComponentGeneric(Type componentType, Actor actor, World world, NetworkSyncDirection direction)
        {
            var method = GetAddComponentMethod(componentType);
            if (method != null)
            {
                try
                {
                    method.Invoke(world, new object[] { actor, direction });
                    return;
                }
                catch
                {
                }
            }

            method = GetSetComponentMethod(componentType);
            if (method != null)
            {
                var defaultValue = Activator.CreateInstance(componentType);
                method.Invoke(world, new object[] { actor, defaultValue });
            }
        }
        
        private static void AddCapabilityToActor(Type capabilityType, Actor actor, World world, SheetInstance instance)
        {
            var method = GetAddCapabilityMethod(capabilityType);

            if (method?.Invoke(world, new object[] { actor }) is Capability capability)
                instance.AddCapability(capability);
        }

        private static MethodInfo GetAddComponentMethod(Type componentType)
        {
            if (componentType == null) return null;
            if (s_AddComponentMethods.TryGetValue(componentType, out var method))
                return method;

            lock (s_MethodCacheLock)
            {
                if (s_AddComponentMethods.TryGetValue(componentType, out method))
                    return method;

                var generic = typeof(World).GetMethod(nameof(World.AddComponent));
                if (generic == null)
                    return null;

                method = generic.MakeGenericMethod(componentType);
                s_AddComponentMethods[componentType] = method;
                return method;
            }
        }

        private static MethodInfo GetSetComponentMethod(Type componentType)
        {
            if (componentType == null) return null;
            if (s_SetComponentMethods.TryGetValue(componentType, out var method))
                return method;

            lock (s_MethodCacheLock)
            {
                if (s_SetComponentMethods.TryGetValue(componentType, out method))
                    return method;

                var generic = typeof(World).GetMethod(nameof(World.SetComponent));
                if (generic == null)
                    return null;

                method = generic.MakeGenericMethod(componentType);
                s_SetComponentMethods[componentType] = method;
                return method;
            }
        }

        private static MethodInfo GetAddCapabilityMethod(Type capabilityType)
        {
            if (capabilityType == null) return null;
            if (s_AddCapabilityMethods.TryGetValue(capabilityType, out var method))
                return method;

            lock (s_MethodCacheLock)
            {
                if (s_AddCapabilityMethods.TryGetValue(capabilityType, out method))
                    return method;

                var generic = typeof(World).GetMethod(nameof(World.AddCapability));
                if (generic == null)
                    return null;

                method = generic.MakeGenericMethod(capabilityType);
                s_AddCapabilityMethods[capabilityType] = method;
                return method;
            }
        }

        internal static MethodInfo GetRemoveCapabilityMethod(Type capabilityType)
        {
            if (capabilityType == null) return null;
            if (s_RemoveCapabilityMethods.TryGetValue(capabilityType, out var method))
                return method;

            lock (s_MethodCacheLock)
            {
                if (s_RemoveCapabilityMethods.TryGetValue(capabilityType, out method))
                    return method;

                var generic = typeof(World).GetMethod(nameof(World.RemoveCapability));
                if (generic == null)
                    return null;

                method = generic.MakeGenericMethod(capabilityType);
                s_RemoveCapabilityMethods[capabilityType] = method;
                return method;
            }
        }
    }
    
    /// <summary>
    ///   <para>能力表单实例</para>
    /// </summary>
    [Serializable]
    public sealed class SheetInstance : IDisposable
    {
        private readonly Actor m_Actor;
        private readonly World m_World;
        private readonly List<Capability> m_Capabilities = new(16);
        private readonly List<SheetInstance> m_SubInstances = new(8);
        private bool m_IsDisposed;

        internal SheetInstance(Actor actor, World world)
        {
            m_Actor = actor;
            m_World = world ?? throw new ArgumentNullException(nameof(world));
        }
        
        ~SheetInstance() => Dispose();

        internal void AddSubInstance(SheetInstance subInstance)
        {
            if (subInstance == null) return;
            m_SubInstances.Add(subInstance);
        }
        
        internal void AddCapability(Capability capability)
        {
            if (capability == null) return;
            m_Capabilities.Add(capability);
        }
        
        /// <summary>
        ///   <para>从Actor中移除表单实例</para>
        /// </summary>
        public void RemoveFromActor()
        {
            if (m_IsDisposed) return;
            
            foreach (var subInstance in m_SubInstances)
                subInstance.RemoveFromActor();
            
            foreach (var capability in m_Capabilities)
            {
                var capabilityType = capability.GetType();
                RemoveCapabilityGeneric(capabilityType, m_Actor, m_World);
            }
            
            m_Capabilities.Clear();
            m_SubInstances.Clear();
        }
        
        private static void RemoveCapabilityGeneric(Type capabilityType, Actor actor, World world)
        {
            var method = CapabilitySheet.GetRemoveCapabilityMethod(capabilityType);
            method?.Invoke(world, new object[] { actor });
        }
        
        public void Dispose()
        {
            if (m_IsDisposed) return;
            
            RemoveFromActor();
            m_IsDisposed = true;
        }
    }
    
    /// <summary>
    ///   <para>表单管理器</para>
    /// </summary>
    [Serializable]
    public sealed class SheetManager
    {
        private readonly World m_World;
        private readonly Dictionary<Actor, List<SheetInstance>> m_ActorSheets = new(128);
        private readonly SpinLock m_Lock;
        
        internal SheetManager(World world)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_Lock = new SpinLock(false);
        }
        
        public SheetInstance ApplySheet(Actor actor, CapabilitySheet sheet, CapabilitySheetApplyMode mode = CapabilitySheetApplyMode.All)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                
                var instance = sheet.ApplyTo(actor, m_World, mode);
                
                if (!m_ActorSheets.TryGetValue(actor, out var instances))
                {
                    instances = new List<SheetInstance>();
                    m_ActorSheets[actor] = instances;
                }
                
                instances.Add(instance);
                return instance;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }
        
        public bool RemoveSheet(Actor actor, SheetInstance instance)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                
                if (!m_ActorSheets.TryGetValue(actor, out var instances))
                    return false;
                
                if (!instances.Remove(instance))
                    return false;
                
                instance.RemoveFromActor();
                
                if (instances.Count == 0)
                    m_ActorSheets.Remove(actor);
                
                return true;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }
        
        public void RemoveAllSheets(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                
                if (!m_ActorSheets.TryGetValue(actor, out var instances))
                    return;
                
                foreach (var instance in instances)
                    instance.RemoveFromActor();
                
                m_ActorSheets.Remove(actor);
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }
        
        public IReadOnlyList<SheetInstance> GetActorSheets(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                return m_ActorSheets.TryGetValue(actor, out var instances) 
                    ? instances.AsReadOnly() 
                    : (IReadOnlyList<SheetInstance>)Array.Empty<SheetInstance>();
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }
        
        public void Clear()
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                
                foreach (var instances in m_ActorSheets.Values)
                {
                    foreach (var instance in instances)
                        instance.RemoveFromActor();
                }
                
                m_ActorSheets.Clear();
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }
    }
    
    #endregion
    
    #region 能力管理器
    
    /// <summary>
    ///   <para>能力管理器</para>
    /// </summary>
    [Serializable]
    public sealed class CapabilityManager : IDisposable
    {
        private readonly World m_World;
        private readonly ActionQueue m_ActionQueue;
        private readonly SheetManager m_SheetManager;
        private readonly Dictionary<Type, Queue<Capability>> m_CapabilityPools = new(32);
        private readonly HashSet<Actor> m_DirtyActors = new(256);
        private readonly SpinLock m_Lock;
        private readonly Func<Capability, bool> m_ShouldActivate;
        private readonly Func<Capability, bool> m_ShouldDeactivate;
        private bool m_IsDisposed;
        
        /// <summary>
        ///   <para>表单管理器（批量管理能力与组件的应用）</para>
        /// </summary>
        public SheetManager Sheets { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_SheetManager; }
        
        /// <summary>
        ///   <para>构造能力管理器</para>
        /// </summary>
        /// <param name="world">所属世界</param>
        internal CapabilityManager(World world)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_ActionQueue = new ActionQueue();
            m_SheetManager = new SheetManager(world);
            m_Lock = new SpinLock(false);
            m_ShouldActivate = CheckCapabilityActivation;
            m_ShouldDeactivate = CheckCapabilityDeactivation;
        }
        
        ~CapabilityManager() => Dispose();

        /// <summary>
        ///   <para>为行动者添加能力</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="actor">行动者</param>
        public T AddCapability<T>(Actor actor) where T : Capability, new()
        {
            if (m_IsDisposed) throw new ObjectDisposedException(nameof(CapabilityManager));

            var capability = GetOrCreateCapability<T>();
            capability.Setup(actor, m_World);

            var instance = new CapabilityInstance
            {
                capability = capability,
                tickGroup = (int)capability.TickGroup,
                tickOrder = capability.TickOrder
            };

            m_World.Actors.AddCapabilityInstance(actor, instance);
            m_ActionQueue.AddCapability(capability, (int)capability.TickGroup, capability.TickOrder);

            MarkActorDirty(actor);

            return capability;
        }

        /// <summary>
        ///   <para>移除行动者上的指定能力</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="actor">行动者</param>
        public bool RemoveCapability<T>(Actor actor) where T : Capability
        {
            if (m_IsDisposed) return false;

            var capabilities = m_World.Actors.GetCapabilities(actor);
            if (capabilities == null) return false;

            for (int i = 0; i < capabilities.Count; i++)
            {
                var instance = capabilities[i];
                if (instance.capability is T)
                {
                    m_ActionQueue.RemoveCapability(instance.capability);

                    if (instance.capability.IsActive)
                    {
                        instance.capability.OnDeactivated();
                        instance.capability.IsActive = false;
                    }

                    m_World.Actors.RemoveCapabilityInstance(actor, instance.capability);
                    ReturnToPool(instance.capability);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>阻塞行动者上指定标签（由发起者记录）</para>
        /// </summary>
        public void BlockTag(Actor actor, TagId tagId, object instigator)
        {
            m_World.Actors.BlockTag(actor, tagId, instigator);
            MarkActorDirty(actor);
        }

        /// <summary>
        ///   <para>解除行动者上指定标签的阻塞</para>
        /// </summary>
        public void UnblockTag(Actor actor, TagId tagId, object instigator)
        {
            m_World.Actors.UnblockTag(actor, tagId, instigator);
            MarkActorDirty(actor);
        }

        /// <summary>
        ///   <para>查询行动者上的标签是否被阻塞</para>
        /// </summary>
        public bool IsTagBlocked(Actor actor, TagId tagId)
        {
            return m_World.Actors.IsTagBlocked(actor, tagId);
        }

        /// <summary>
        ///   <para>更新能力系统</para>
        /// </summary>
        public void Update(float deltaTime)
        {
            if (m_IsDisposed) return;

            ProcessDirtyActors();

            m_ActionQueue.Update(deltaTime, m_ShouldActivate, m_ShouldDeactivate, m_World.Actors.EnableMultiThreading);
        }
        
        /// <summary>
        ///   <para>更新指定分组的能力系统</para>
        /// </summary>
        public void Update(float deltaTime, TickGroup tickGroup)
        {
            if (m_IsDisposed) return;

            ProcessDirtyActors();

            m_ActionQueue.Update(deltaTime, 
                m_ShouldActivate,
                m_ShouldDeactivate,
                (int)tickGroup,
                m_World.Actors.EnableMultiThreading);
        }

        /// <summary>
        ///   <para>清理能力系统（清空队列、表单与脏标记）</para>
        /// </summary>
        public void Clear()
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                m_ActionQueue.Clear();
                m_SheetManager.Clear();
                m_DirtyActors.Clear();
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        ///   <para>释放能力管理器</para>
        /// </summary>
        public void Dispose()
        {
            if (m_IsDisposed) return;

            Clear();

            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                
                foreach (var pool in m_CapabilityPools.Values)
                {
                    while (pool.Count > 0)
                        pool.Dequeue().Dispose();
                }

                m_CapabilityPools.Clear();
                m_DirtyActors.Clear();

                m_IsDisposed = true;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }

            GC.SuppressFinalize(this);
        }
        
        private T GetOrCreateCapability<T>() where T : Capability, new()
        {
            var type = typeof(T);

            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                
                if (m_CapabilityPools.TryGetValue(type, out var pool) && pool.Count > 0)
                    return (T)pool.Dequeue();
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }

            return new T();
        }

        private void ReturnToPool(Capability capability)
        {
            var type = capability.GetType();

            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                
                if (!m_CapabilityPools.TryGetValue(type, out var pool))
                {
                    pool = new Queue<Capability>();
                    m_CapabilityPools[type] = pool;
                }

                if (pool.Count < 100)
                    pool.Enqueue(capability);
                else
                    capability.Dispose();
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        public void MarkActorDirty(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                m_DirtyActors.Add(actor);
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        private void ProcessDirtyActors()
        {
            if (m_DirtyActors.Count == 0) return;

            Actor[] actorsToProcess;
            int actorCount;
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);

                actorCount = m_DirtyActors.Count;
                actorsToProcess = actorCount == 0
                    ? Array.Empty<Actor>()
                    : ArrayPool<Actor>.Shared.Rent(actorCount);

                int index = 0;
                foreach (var actor in m_DirtyActors)
                {
                    if (index >= actorCount) break;
                    actorsToProcess[index++] = actor;
                }

                m_DirtyActors.Clear();
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }

            for (int i = 0; i < actorCount; i++)
            {
                var actor = actorsToProcess[i];
                var capabilities = m_World.Actors.GetCapabilities(actor);
                if (capabilities == null || capabilities.Count == 0) continue;

                for (int j = 0; j < capabilities.Count; j++)
                {
                    var capability = capabilities[j].capability;
                    if (capability == null || capability.IsDisposed) continue;
                    m_ActionQueue.MarkCapabilityForActivationCheck(capability);
                }
            }

            if (actorCount > 0 && actorsToProcess.Length > 0)
            {
                ArrayPool<Actor>.Shared.Return(actorsToProcess, false);
            }
        }

        private bool CheckCapabilityActivation(Capability capability)
        {
            var owner = capability.OwnerActor;
            foreach (var tag in capability.Tags)
            {
                if (m_World.Actors.IsTagBlocked(owner, tag))
                    return false;
            }

            return capability.ShouldActivate();
        }

        private bool CheckCapabilityDeactivation(Capability capability)
        {
            var owner = capability.OwnerActor;
            foreach (var tag in capability.Tags)
            {
                if (m_World.Actors.IsTagBlocked(owner, tag))
                    return true;
            }

            return capability.ShouldDeactivate();
        }
    }
    
    #endregion
    
    #region 网络同步系统（Beta）
    
    /// <summary>
    ///   <para>网络传输接口</para>
    /// </summary>
    public interface INetworkSyncTransport : IDisposable
    {
        /// <summary>
        ///   <para>当前是否处于可用连接状态</para>
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        ///   <para>接收一帧网络数据（非阻塞，返回0表示当前无数据）</para>
        /// </summary>
        /// <param name="buffer">用于接收数据的缓冲区</param>
        /// <returns>实际接收的字节数，若为0表示当前无数据</returns>
        int Receive(Span<byte> buffer);
        /// <summary>
        ///   <para>发送一帧网络数据</para>
        /// </summary>
        /// <param name="buffer">待发送数据缓冲区</param>
        void Send(ReadOnlySpan<byte> buffer);
    }

    /// <summary>
    ///   <para>网络同步收发方向</para>
    /// </summary>
    [Serializable, Flags]
    public enum NetworkSyncDirection : byte
    {
        /// <summary>
        ///   <para>不进行发送或接收</para>
        /// </summary>
        None = 0,
        /// <summary>
        ///   <para>仅发送本地数据</para>
        /// </summary>
        Send = 1 << 0,
        /// <summary>
        ///   <para>仅接收远端数据</para>
        /// </summary>
        Receive = 1 << 1,
        /// <summary>
        ///   <para>同时发送与接收</para>
        /// </summary>
        SendAndReceive = Send | Receive,
    }

    /// <summary>
    ///   <para>网络同步配置</para>
    /// </summary>
    [Serializable]
    public readonly struct NetworkSyncOptions
    {
        private const int MIN_PACKET_SIZE = 512;            // 最小数据包大小
        private const int MAX_PACKET_SIZE = 256 * 1024;     // 最大数据包大小
        private const int DEFAULT_PACKET_SIZE = 64 * 1024;  // 默认数据包大小
        
        /// <summary>
        ///   <para>全局收发方向</para>
        /// </summary>
        public readonly NetworkSyncDirection direction;
        /// <summary>
        ///   <para>发送间隔（秒）</para>
        /// </summary>
        public readonly float sendInterval;
        /// <summary>
        ///   <para>接收间隔（秒）</para>
        /// </summary>
        public readonly float receiveInterval;
        /// <summary>
        ///   <para>发送时使用的Tick组</para>
        /// </summary>
        public readonly TickGroup sendTickGroup;
        /// <summary>
        ///   <para>接收时使用的Tick组</para>
        /// </summary>
        public readonly TickGroup receiveTickGroup;
        /// <summary>
        /// <para>最大数据包大小（字节）</para>
        /// </summary>
        public readonly int maxPacketSize;
        /// <summary>
        ///   <para>数据包标识符</para>
        /// </summary>
        public readonly byte packetMagic;
        /// <summary>
        ///   <para>数据包版本</para>
        /// </summary>
        public readonly byte packetVersion;

        /// <summary>
        ///   <para>是否启用发送</para>
        /// </summary>
        public bool EnableSend
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (direction & NetworkSyncDirection.Send) != 0;
        }

        /// <summary>
        ///   <para>是否启用接收</para>
        /// </summary>
        public bool EnableReceive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (direction & NetworkSyncDirection.Receive) != 0;
        }
        
        public NetworkSyncOptions(
            NetworkSyncDirection direction,
            float sendInterval = 0.05f,
            float receiveInterval = 0.05f,
            TickGroup sendTickGroup = TickGroup.Late,
            TickGroup receiveTickGroup = TickGroup.Early,
            int maxPacketSize = DEFAULT_PACKET_SIZE,
            byte packetMagic = 0xAC,
            byte packetVersion = 1
            )
        {
            this.direction = direction;
            this.sendInterval = Math.Max(sendInterval, 0f);
            this.receiveInterval = Math.Max(receiveInterval, 0f);
            this.sendTickGroup = sendTickGroup;
            this.receiveTickGroup = receiveTickGroup;
            this.maxPacketSize = Math.Clamp(maxPacketSize <= 0 ? DEFAULT_PACKET_SIZE : maxPacketSize, MIN_PACKET_SIZE, MAX_PACKET_SIZE);
            this.packetMagic = packetMagic;
            this.packetVersion = packetVersion;
        }
    }

    /// <summary>
    ///   <para>网络同步写入器</para>
    /// </summary>
    internal ref struct NetworkSyncWriter
    {
        private readonly Span<byte> m_Buffer;
        private int m_Length;
        private bool m_HasError;

        public NetworkSyncWriter(Span<byte> buffer)
        {
            m_Buffer = buffer;
            m_Length = 0;
            m_HasError = false;
        }

        public int Length { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_HasError ? 0 : m_Length; }

        public bool HasError { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_HasError; }

        public ReadOnlySpan<byte> WrittenSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_HasError ? ReadOnlySpan<byte>.Empty : m_Buffer.Slice(0, m_Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReserve(int size)
        {
            if (m_HasError) return false;
            if (size < 0 || m_Length + size > m_Buffer.Length)
            {
                m_HasError = true;
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            if (!TryReserve(1)) return;
            m_Buffer[m_Length++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value)
        {
            if (!TryReserve(2)) return;
            BinaryPrimitives.WriteInt16LittleEndian(m_Buffer.Slice(m_Length, 2), value);
            m_Length += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            if (!TryReserve(4)) return;
            BinaryPrimitives.WriteInt32LittleEndian(m_Buffer.Slice(m_Length, 4), value);
            m_Length += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long value)
        {
            if (!TryReserve(8)) return;
            BinaryPrimitives.WriteInt64LittleEndian(m_Buffer.Slice(m_Length, 8), value);
            m_Length += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ReadOnlySpan<byte> value)
        {
            if (!TryReserve(value.Length)) return;
            value.CopyTo(m_Buffer.Slice(m_Length, value.Length));
            m_Length += value.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int size)
        {
            if (!TryReserve(size)) return Span<byte>.Empty;
            var span = m_Buffer.Slice(m_Length, size);
            m_Length += size;
            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32At(int position, int value)
        {
            if (m_HasError) return;
            if (position < 0 || position + 4 > m_Buffer.Length)
            {
                m_HasError = true;
                return;
            }
            BinaryPrimitives.WriteInt32LittleEndian(m_Buffer.Slice(position, 4), value);
        }
    }

    /// <summary>
    ///   <para>网络同步读取器</para>
    /// </summary>
    internal ref struct NetworkSyncReader
    {
        private ReadOnlySpan<byte> m_Buffer;
        private int m_Position;
        private bool m_HasError;

        public NetworkSyncReader(ReadOnlySpan<byte> buffer)
        {
            m_Buffer = buffer;
            m_Position = 0;
            m_HasError = false;
        }

        public bool HasError { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_HasError; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryConsume(int size)
        {
            if (m_HasError) return false;
            if (size < 0 || m_Position + size > m_Buffer.Length)
            {
                m_HasError = true;
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (!TryConsume(1)) return 0;
            return m_Buffer[m_Position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            if (!TryConsume(2)) return 0;
            short value = BinaryPrimitives.ReadInt16LittleEndian(m_Buffer.Slice(m_Position, 2));
            m_Position += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            if (!TryConsume(4)) return 0;
            int value = BinaryPrimitives.ReadInt32LittleEndian(m_Buffer.Slice(m_Position, 4));
            m_Position += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            if (!TryConsume(8)) return 0;
            long value = BinaryPrimitives.ReadInt64LittleEndian(m_Buffer.Slice(m_Position, 8));
            m_Position += 8;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadBytes(int length)
        {
            if (!TryConsume(length)) return ReadOnlySpan<byte>.Empty;
            var span = m_Buffer.Slice(m_Position, length);
            m_Position += length;
            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int length)
        {
            if (!TryConsume(length)) return;
            m_Position += length;
        }
    }
    
    /// <summary>
    ///   <para>网络同步包头格式化器</para>
    /// </summary>
    public interface INetworkPacketHeaderFormatter
    {
        /// <summary>
        ///   <para>写入网络同步包头</para>
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="options">网络同步选项</param>
        /// <param name="sequence">序列号</param>
        int WriteHeader(Span<byte> buffer, in NetworkSyncOptions options, int sequence);
        /// <summary>
        ///   <para>尝试读取网络同步包头</para>
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="options">网络同步选项</param>
        /// <param name="headerSize">包头大小</param>
        bool TryReadHeader(ReadOnlySpan<byte> buffer, in NetworkSyncOptions options, out int headerSize, out int sequence);
    }

    /// <summary>
    ///   <para>默认网络同步包头格式化器</para>
    /// </summary>
    internal sealed class DefaultNetworkPacketHeaderFormatter : InstanceBase<DefaultNetworkPacketHeaderFormatter>, INetworkPacketHeaderFormatter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int WriteHeader(Span<byte> buffer, in NetworkSyncOptions options, int sequence)
        {
            if (buffer.Length < 8) return 0;
            buffer[0] = options.packetMagic;
            buffer[1] = options.packetVersion;
            BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(2, 2), 0);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4, 4), sequence);
            return 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadHeader(ReadOnlySpan<byte> buffer, in NetworkSyncOptions options, out int headerSize, out int sequence)
        {
            headerSize = 0;
            sequence = 0;
            if (buffer.Length < 8) return false;
            byte magic = buffer[0];
            byte version = buffer[1];
            if (magic != options.packetMagic || version != options.packetVersion)
                return false;
            sequence = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4, 4));
            headerSize = 8;
            return true;
        }
    }

    /// <summary>
    ///   <para>网络组件序列化器</para>
    /// </summary>
    public interface INetworkComponentSerializer
    {
        /// <summary>
        ///   <para>获取组件大小</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="value">组件实例</param>
        int GetSize<T>(in T value) where T : struct, IComponent;
        /// <summary>
        ///   <para>写入组件数据</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="buffer">缓冲区</param>
        /// <param name="value">组件实例</param>
        void Write<T>(Span<byte> buffer, in T value) where T : struct, IComponent;
        /// <summary>
        ///   <para>读取组件数据</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="buffer">缓冲区</param>
        /// <param name="value">组件实例</param>
        void Read<T>(ReadOnlySpan<byte> buffer, ref T value) where T : struct, IComponent;
    }

    /// <summary>
    ///   <para>自定义二进制网络组件序列化器</para>
    /// </summary>
    internal sealed class BinaryNetworkComponentSerializer : InstanceBase<BinaryNetworkComponentSerializer>, INetworkComponentSerializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSize<T>(in T value) where T : struct, IComponent
        {
            var temp = value;
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref temp, 1));
            return span.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(Span<byte> buffer, in T value) where T : struct, IComponent
        {
            var temp = value;
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref temp, 1));
            span.CopyTo(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read<T>(ReadOnlySpan<byte> buffer, ref T value) where T : struct, IComponent
        {
            value = default;
            if (buffer.IsEmpty)
                return;

            var valueSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
            int length = buffer.Length <= valueSpan.Length ? buffer.Length : valueSpan.Length;
            buffer.Slice(0, length).CopyTo(valueSpan);
        }
    }

    /// <summary>
    ///   <para>网络同步类型ID</para>
    /// </summary>
    internal static class NetworkSyncTypeId
    {
        /// <summary>
        ///   <para>获取类型ID</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Get(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var name = type.FullName;
            if (string.IsNullOrEmpty(name))
                name = type.Name;

            unchecked
            {
                int hash = 23;
                for (int i = 0; i < name.Length; i++)
                    hash = hash * 31 + name[i];
                return hash;
            }
        }
    }

    /// <summary>
    ///   <para>网络同步组件处理接口</para>
    /// </summary>
    internal interface IComponentSyncHandler
    {
        int NetworkTypeId { get; }
        NetworkSyncDirection Direction { get; }
        void RegisterActor(in Actor actor);
        void UnregisterActor(in Actor actor);
        void MarkDirty(in Actor actor);
        bool WriteToPacket(World world, ref NetworkSyncWriter writer, INetworkComponentSerializer serializer, NetworkSyncManager manager);
        void ReadFromPacket(World world, ref NetworkSyncReader reader, INetworkComponentSerializer serializer, NetworkSyncManager manager);
    }

    /// <summary>
    ///   <para>网络同步组件处理类</para>
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    internal sealed class ComponentSyncHandler<T> : IComponentSyncHandler
        where T : struct, IComponent
    {
        private readonly int m_NetworkTypeId;
        private readonly NetworkSyncDirection m_Direction;
        private Actor[] m_ReplicatedActors;
        private int m_ReplicatedCount;
        private Actor[] m_DirtyActors;
        private int m_DirtyCount;

        internal ComponentSyncHandler(NetworkSyncDirection direction)
        {
            m_NetworkTypeId = NetworkSyncTypeId.Get(typeof(T));
            m_Direction = direction;
        }

        public int NetworkTypeId { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_NetworkTypeId; }
        public NetworkSyncDirection Direction { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Direction; }

        public void RegisterActor(in Actor actor)
        {
            m_ReplicatedActors ??= new Actor[16];

            long id = actor.id;
            for (int i = 0; i < m_ReplicatedCount; i++)
            {
                if (m_ReplicatedActors[i].id == id)
                    return;
            }

            if (m_ReplicatedCount >= m_ReplicatedActors.Length)
            {
                int newLength = m_ReplicatedActors.Length << 1;
                Array.Resize(ref m_ReplicatedActors, newLength);
            }

            m_ReplicatedActors[m_ReplicatedCount++] = actor;
        }

        public void UnregisterActor(in Actor actor)
        {
            if (m_ReplicatedActors == null || m_ReplicatedCount == 0)
                return;

            long id = actor.id;
            for (int i = 0; i < m_ReplicatedCount; i++)
            {
                if (m_ReplicatedActors[i].id != id)
                    continue;

                int last = --m_ReplicatedCount;
                if (i != last)
                    m_ReplicatedActors[i] = m_ReplicatedActors[last];
                break;
            }
        }

        public void MarkDirty(in Actor actor)
        {
            m_DirtyActors ??= new Actor[64];

            long id = actor.id;
            for (int i = 0; i < m_DirtyCount; i++)
            {
                if (m_DirtyActors[i].id == id)
                    return;
            }

            if (m_DirtyCount >= m_DirtyActors.Length)
            {
                int newLength = m_DirtyActors.Length << 1;
                Array.Resize(ref m_DirtyActors, newLength);
            }

            m_DirtyActors[m_DirtyCount++] = actor;
        }

        public bool WriteToPacket(World world, ref NetworkSyncWriter writer, INetworkComponentSerializer serializer, NetworkSyncManager manager)
        {
            if (m_DirtyCount == 0) return false;

            int entryCount = 0;
            for (int i = 0; i < m_DirtyCount; i++)
            {
                var actor = m_DirtyActors[i];
                if (!world.IsActorAlive(actor)) continue;
                if (!IsActorReplicated(actor.id)) continue;
                entryCount++;
            }

            if (entryCount == 0)
            {
                m_DirtyCount = 0;
                return false;
            }

            writer.WriteInt32(m_NetworkTypeId);
            writer.WriteInt32(entryCount);

            int written = 0;
            for (int i = 0; i < m_DirtyCount; i++)
            {
                var actor = m_DirtyActors[i];
                if (!world.IsActorAlive(actor))
                    continue;

                if (!IsActorReplicated(actor.id))
                    continue;

                long networkId = manager.GetOrCreateNetworkEntityId(actor);
                writer.WriteInt64(networkId);
                ref var component = ref world.GetComponent<T>(actor);
                int size = serializer.GetSize(in component);
                writer.WriteInt32(size);

                if (size > 0)
                {
                    var span = writer.GetSpan(size);
                    if (span.Length == size)
                    {
                        serializer.Write(span, in component);
                    }
                }

                written++;

                if (writer.HasError)
                    break;
                if (written >= entryCount)
                    break;
            }

            m_DirtyCount = 0;
            return !writer.HasError && entryCount > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsActorReplicated(long id)
        {
            if (m_ReplicatedActors == null || m_ReplicatedCount == 0)
                return false;
            for (int i = 0; i < m_ReplicatedCount; i++)
            {
                if (m_ReplicatedActors[i].id == id)
                    return true;
            }
            return false;
        }

        public void ReadFromPacket(World world, ref NetworkSyncReader reader, INetworkComponentSerializer serializer, NetworkSyncManager manager)
        {
            int entryCount = reader.ReadInt32();
            if (reader.HasError || entryCount <= 0)
                return;

            for (int i = 0; i < entryCount; i++)
            {
                long actorId = reader.ReadInt64();
                if (reader.HasError)
                    return;

                int size = reader.ReadInt32();
                if (reader.HasError || size < 0)
                    return;

                var actor = manager.ResolveActorFromNetworkId(actorId);
                if (!world.IsActorAlive(actor))
                {
                    if (size > 0)
                    {
                        reader.Skip(size);
                        if (reader.HasError)
                            return;
                    }
                    continue;
                }

                ReadOnlySpan<byte> payload = size > 0 ? reader.ReadBytes(size) : ReadOnlySpan<byte>.Empty;
                if (reader.HasError)
                    return;

                bool hasComponent = world.HasComponent<T>(actor);
                if (!hasComponent)
                {
                    ref var added = ref world.AddComponent<T>(actor);
                    serializer.Read(payload, ref added);
                }
                else
                {
                    ref var existing = ref world.GetComponent<T>(actor);
                    serializer.Read(payload, ref existing);
                }

                if (reader.HasError)
                    return;
            }
        }
    }

    /// <summary>
    ///   <para>网络同步管理器</para>
    /// </summary>
    internal sealed class NetworkSyncManager : IDisposable
    {
        private readonly World m_World;
        private readonly NetworkSyncOptions m_Options;
        private readonly Dictionary<int, IComponentSyncHandler> m_ComponentHandlers;
        private readonly Dictionary<long, Actor> m_RemoteActors = new Dictionary<long, Actor>(128);
        private readonly Dictionary<long, long> m_ActorToNetworkIds = new Dictionary<long, long>(128);
        private readonly byte[] m_SendBuffer;
        private readonly byte[] m_ReceiveBuffer;
        private readonly object m_SyncLock = new object();

        private INetworkSyncTransport m_Transport;
        private INetworkComponentSerializer m_ComponentSerializer;
        private INetworkPacketHeaderFormatter m_HeaderFormatter;
        private int m_Sequence;
        private long m_NextLocalNetworkId = -1;
        private float m_SendTimer;
        private float m_ReceiveTimer;
        private bool m_IsDisposed;

        internal NetworkSyncManager(World world, NetworkSyncOptions options)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_Options = options;

            m_SendBuffer = new byte[options.maxPacketSize];
            m_ReceiveBuffer = new byte[options.maxPacketSize];
            m_ComponentHandlers = new Dictionary<int, IComponentSyncHandler>(16);
            m_ComponentSerializer = BinaryNetworkComponentSerializer.Instance;
            m_HeaderFormatter = DefaultNetworkPacketHeaderFormatter.Instance;
        }

        internal Actor ResolveActorFromNetworkId(long networkId)
        {
            if (networkId == 0)
                return Actor.none;

            lock (m_SyncLock)
            {
                if (m_RemoteActors.TryGetValue(networkId, out var mapped) && m_World.IsActorAlive(mapped))
                    return mapped;

                var actor = m_World.CreateActor();
                m_RemoteActors[networkId] = actor;
                m_ActorToNetworkIds[actor.id] = networkId;
                return actor;
            }
        }

        internal long GetOrCreateNetworkEntityId(in Actor actor)
        {
            long actorId = actor.id;
            if (actor.IsNone || actorId == 0)
                return 0;
            lock (m_SyncLock)
            {
                if (m_ActorToNetworkIds.TryGetValue(actorId, out var existing))
                    return existing;

                long networkId = m_NextLocalNetworkId--;
                m_ActorToNetworkIds[actorId] = networkId;
                m_RemoteActors[networkId] = actor;
                return networkId;
            }
        }

        internal void OnActorDestroyed(in Actor actor)
        {
            lock (m_SyncLock)
            {
                long actorId = actor.id;
                if (actorId != 0 && m_ActorToNetworkIds.TryGetValue(actorId, out var networkId))
                {
                    m_ActorToNetworkIds.Remove(actorId);
                    m_RemoteActors.Remove(networkId);
                }

                foreach (var handler in m_ComponentHandlers.Values)
                {
                    handler?.UnregisterActor(actor);
                }
            }
        }
        
        ~NetworkSyncManager() => Dispose();

        public void Dispose()
        {
            if (m_IsDisposed) return;

            lock (m_SyncLock)
            {
                if (m_IsDisposed) return;

                try
                {
                    m_Transport?.Dispose();
                }
                finally
                {
                    m_Transport = null;
                    m_ComponentHandlers.Clear();
                    m_RemoteActors.Clear();
                    m_ActorToNetworkIds.Clear();
                    m_IsDisposed = true;
                }
            }
        }

        public void RegisterReplicatedComponent<T>(NetworkSyncDirection direction, in Actor actor)
            where T : struct, IComponent
        {
            if (m_IsDisposed) return;
            int networkTypeId = NetworkSyncTypeId.Get(typeof(T));

            lock (m_SyncLock)
            {
                if (!m_ComponentHandlers.TryGetValue(networkTypeId, out var handler))
                {
                    handler = new ComponentSyncHandler<T>(direction);
                    m_ComponentHandlers[networkTypeId] = handler;
                }

                handler.RegisterActor(actor);
                handler.MarkDirty(actor);
            }
        }

        public void UnregisterReplicatedComponent<T>(in Actor actor)
            where T : struct, IComponent
        {
            int networkTypeId = NetworkSyncTypeId.Get(typeof(T));
            lock (m_SyncLock)
            {
                if (m_ComponentHandlers.TryGetValue(networkTypeId, out var handler))
                {
                    handler.UnregisterActor(actor);
                }
            }
        }

        public void MarkComponentDirty<T>(in Actor actor)
            where T : struct, IComponent
        {
            if (m_IsDisposed) return;

            int networkTypeId = NetworkSyncTypeId.Get(typeof(T));

            lock (m_SyncLock)
            {
                if (m_ComponentHandlers.TryGetValue(networkTypeId, out var handler))
                {
                    handler.MarkDirty(actor);
                }
            }
        }

        public void SetTransport(INetworkSyncTransport transport)
        {
            if (m_IsDisposed)
            {
                transport?.Dispose();
                return;
            }

            lock (m_SyncLock)
            {
                if (ReferenceEquals(m_Transport, transport)) return;

                var old = m_Transport;
                m_Transport = transport;
                m_SendTimer = 0f;
                m_ReceiveTimer = 0f;
                m_Sequence = 0;

                old?.Dispose();
            }
        }

        internal void SetSerializer(INetworkComponentSerializer serializer)
        {
            if (m_IsDisposed) return;
            lock (m_SyncLock)
            {
                if (ReferenceEquals(m_ComponentSerializer, serializer)) return;
                m_ComponentSerializer = serializer ?? BinaryNetworkComponentSerializer.Instance;
            }
        }

        internal void SetHeaderFormatter(INetworkPacketHeaderFormatter formatter)
        {
            if (m_IsDisposed) return;
            lock (m_SyncLock)
            {
                if (ReferenceEquals(m_HeaderFormatter, formatter)) return;
                m_HeaderFormatter = formatter ?? DefaultNetworkPacketHeaderFormatter.Instance;
            }
        }
        
        internal void BeforeTick(float deltaTime, TickGroup tickGroup)
        {
            if (m_IsDisposed) return;
            if (!m_Options.EnableReceive) return;
            if (tickGroup != m_Options.receiveTickGroup) return;

            m_ReceiveTimer += deltaTime;
            if (m_ReceiveTimer < m_Options.receiveInterval)
                return;
            m_ReceiveTimer = 0f;

            while (true)
            {
                int received;
                INetworkSyncTransport transport;
                INetworkPacketHeaderFormatter headerFormatter;

                lock (m_SyncLock)
                {
                    transport = m_Transport;
                    headerFormatter = m_HeaderFormatter;
                    if (transport == null || !transport.IsConnected)
                        break;

                    received = transport.Receive(m_ReceiveBuffer);
                }

                if (received <= 0)
                    break;

                var span = new ReadOnlySpan<byte>(m_ReceiveBuffer, 0, received);
                if (!headerFormatter.TryReadHeader(span, in m_Options, out int headerSize, out int sequence))
                    break;
                if (headerSize < 0 || headerSize > span.Length)
                    break;

                var reader = new NetworkSyncReader(span.Slice(headerSize));
                ProcessIncoming(ref reader);
                if (reader.HasError)
                    break;
            }
        }
        
        internal void AfterTick(float deltaTime, TickGroup tickGroup)
        {
            if (m_IsDisposed) return;
            if (!m_Options.EnableSend) return;
            if (tickGroup != m_Options.sendTickGroup) return;
            if (m_ComponentHandlers.Count == 0) return;

            m_SendTimer += deltaTime;
            if (m_SendTimer < m_Options.sendInterval)
                return;
            m_SendTimer = 0f;

            INetworkSyncTransport transport;
            INetworkPacketHeaderFormatter headerFormatter;
            INetworkComponentSerializer serializer;
            lock (m_SyncLock)
            {
                transport = m_Transport;
                if (transport == null || !transport.IsConnected)
                    return;
                headerFormatter = m_HeaderFormatter;
                serializer = m_ComponentSerializer;
            }

            var buffer = m_SendBuffer.AsSpan();
            int sequence = Interlocked.Increment(ref m_Sequence);
            int headerSize = headerFormatter.WriteHeader(buffer, in m_Options, sequence);
            if (headerSize <= 0 || headerSize >= buffer.Length)
                return;

            var writer = new NetworkSyncWriter(buffer.Slice(headerSize));

            int typeCountPosition = writer.Length;
            writer.WriteInt32(0);

            int typeCount = 0;

            lock (m_SyncLock)
            {
                foreach (var handler in m_ComponentHandlers.Values)
                {
                    if (handler == null) continue;
                    if ((handler.Direction & NetworkSyncDirection.Send) == 0)
                        continue;

                    int beforeLength = writer.Length;
                    bool wrote = handler.WriteToPacket(m_World, ref writer, serializer, this);
                    if (writer.HasError)
                        break;

                    if (wrote && writer.Length > beforeLength)
                        typeCount++;
                }
            }

            if (writer.HasError || typeCount == 0)
                return;

            writer.WriteInt32At(typeCountPosition, typeCount);

            int totalLength = headerSize + writer.Length;
            if (totalLength <= 0 || totalLength > buffer.Length)
                return;

            lock (m_SyncLock)
            {
                transport = m_Transport;
                if (transport == null || !transport.IsConnected)
                    return;

                transport.Send(new ReadOnlySpan<byte>(m_SendBuffer, 0, totalLength));
            }
        }

        private void ProcessIncoming(ref NetworkSyncReader reader)
        {
            int typeCount = reader.ReadInt32();
            if (reader.HasError || typeCount <= 0)
                return;

            for (int i = 0; i < typeCount; i++)
            {
                int typeId = reader.ReadInt32();
                if (reader.HasError)
                    return;

                IComponentSyncHandler handler;
                lock (m_SyncLock)
                {
                    m_ComponentHandlers.TryGetValue(typeId, out handler);
                }

                if (handler == null || (handler.Direction & NetworkSyncDirection.Receive) == 0)
                {
                    SkipComponentSegment(ref reader);
                    if (reader.HasError)
                        return;
                    continue;
                }

                handler.ReadFromPacket(m_World, ref reader, m_ComponentSerializer, this);
                if (reader.HasError)
                    return;
            }
        }

        private static void SkipComponentSegment(ref NetworkSyncReader reader)
        {
            int entryCount = reader.ReadInt32();
            if (reader.HasError || entryCount <= 0)
                return;

            for (int i = 0; i < entryCount; i++)
            {
                reader.ReadInt64();
                int size = reader.ReadInt32();
                if (reader.HasError)
                    return;

                if (size > 0)
                {
                    reader.Skip(size);
                    if (reader.HasError)
                        return;
                }
            }
        }
    }
    
    #endregion
    
    #region 世界系统
    
    /// <summary>
    ///   <para>游戏世界（实体、组件、能力与表单的统一入口）</para>
    /// </summary>
    [Serializable, DebuggerDisplay("{ToString}")]
    public sealed class World : IDisposable
    {
        private float m_TimeScale = 1f;
        private readonly NetworkSyncManager m_NetworkSync;
        
        /// <summary>
        ///   <para>世界名称</para>
        /// </summary>
        public string Name { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
        
        /// <summary>
        ///   <para>行动者管理器</para>
        /// </summary>
        public ActorManager Actors { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
        
        /// <summary>
        ///   <para>能力管理器</para>
        /// </summary>
        public CapabilityManager Capabilities { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
        
        /// <summary>
        ///   <para>表单管理器</para>
        /// </summary>
        public SheetManager Sheets { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Capabilities.Sheets; }

        /// <summary>
        ///   <para>是否已释放</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] private set;
        }

        /// <summary>
        ///   <para>时间缩放（更新速率）</para>
        /// </summary>
        public float TimeScale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_TimeScale;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => m_TimeScale = Math.Max(0, value);
        }

        internal World(string name, ActorManagerOptions? actorManagerOptions = null, NetworkSyncOptions? networkSyncOptions = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unnamed World" : name;
            Actors = new ActorManager(actorManagerOptions ?? ActorManagerOptions.Default);
            Capabilities = new CapabilityManager(this);
            m_NetworkSync = networkSyncOptions.HasValue
                ? new NetworkSyncManager(this, networkSyncOptions.Value)
                : null;
        }

        ~World() => Dispose();

        /// <summary>
        ///   <para>创建一个行动者</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Actor CreateActor() => Actors.CreateActor();
        
        /// <summary>
        ///   <para>批量创建行动者</para>
        /// </summary>
        /// <param name="count">数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Actor[] CreateActors(int count) => Actors.CreateActors(count);
        
        /// <summary>
        ///   <para>销毁一个行动者</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyActor(Actor actor)
        {
            m_NetworkSync?.OnActorDestroyed(actor);
            Actors.DestroyActor(actor);
        }
        
        /// <summary>
        ///   <para>为行动者添加组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        /// <param name="direction">网络同步方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddComponent<T>(Actor actor, NetworkSyncDirection direction = NetworkSyncDirection.None)
            where T : struct, IComponent
        {
            ref var component = ref Actors.AddComponent<T>(actor);
            Capabilities.MarkActorDirty(actor);
            if (direction != NetworkSyncDirection.None)
            {
                m_NetworkSync?.RegisterReplicatedComponent<T>(direction, actor);
                MarkComponentDirty<T>(actor);
            }
            return ref component;
        }

        /// <summary>
        ///   <para>获取行动者组件引用</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(Actor actor)
            where T : struct, IComponent
            => ref Actors.GetComponent<T>(actor);
        
        /// <summary>
        ///   <para>获取或添加行动者组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        /// <param name="direction">网络同步方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetOrAddComponent<T>(Actor actor, NetworkSyncDirection direction = NetworkSyncDirection.None)
            where T : struct, IComponent
        {
            if (HasComponent<T>(actor))
                return ref GetComponent<T>(actor);
            return ref AddComponent<T>(actor, direction);
        }
        
        /// <summary>
        ///   <para>尝试获取行动者组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        /// <param name="component">组件数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(Actor actor, out T component)
            where T : struct, IComponent
            => Actors.TryGetComponent(actor, out component);
        
        /// <summary>
        ///   <para>移除行动者组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent<T>(Actor actor)
            where T : struct, IComponent
        {
            bool removed = Actors.RemoveComponent<T>(actor);
            if (removed)
            {
                UnregisterReplicatedComponent<T>(actor);
                Capabilities.MarkActorDirty(actor);
            }
            return removed;
        }
        
        /// <summary>
        ///   <para>设置行动者组件数据</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        /// <param name="component">组件数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(Actor actor, in T component)
            where T : struct, IComponent
        {
            Actors.SetComponent(actor, component);
            Capabilities.MarkActorDirty(actor);
            MarkComponentDirty<T>(actor);
        }
        
        /// <summary>
        ///   <para>判断行动者是否拥有组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Actor actor)
            where T : struct, IComponent
            => Actors.HasComponent<T>(actor);
        
        /// <summary>
        ///   <para>判断行动者是否存活</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActorAlive(Actor actor) => Actors.IsActorAlive(actor);
        
        /// <summary>
        ///   <para>添加能力</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T AddCapability<T>(Actor actor)
            where T : Capability, new()
            => Capabilities.AddCapability<T>(actor);
        
        /// <summary>
        ///   <para>移除指定能力</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveCapability<T>(Actor actor)
            where T : Capability
            => Capabilities.RemoveCapability<T>(actor);
        
        /// <summary>
        ///   <para>阻塞标签</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="tagId">标签ID</param>
        /// <param name="instigator">触发者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BlockTag(Actor actor, TagId tagId, object instigator)
            => Capabilities.BlockTag(actor, tagId, instigator);
        
        /// <summary>
        ///   <para>解除阻塞标签</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="tagId">标签ID</param>
        /// <param name="instigator">触发者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnblockTag(Actor actor, TagId tagId, object instigator)
            => Capabilities.UnblockTag(actor, tagId, instigator);
        
        /// <summary>
        ///   <para>查询标签是否被阻塞</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="tagId">标签ID</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTagBlocked(Actor actor, TagId tagId)
            => Capabilities.IsTagBlocked(actor, tagId);
        
        /// <summary>
        ///   <para>应用表单</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="sheet">表单</param>
        /// <param name="mode">应用模式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SheetInstance ApplySheet(Actor actor, CapabilitySheet sheet, CapabilitySheetApplyMode mode = CapabilitySheetApplyMode.All)
            => Sheets.ApplySheet(actor, sheet, mode);
        
        /// <summary>
        ///   <para>移除指定表单实例</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="instance">表单实例</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveSheet(Actor actor, SheetInstance instance)
            => Sheets.RemoveSheet(actor, instance);
        
        /// <summary>
        ///   <para>移除全部表单</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllSheets(Actor actor)
            => Sheets.RemoveAllSheets(actor);
        
        /// <summary>
        ///   <para>获取表单实例列表</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<SheetInstance> GetActorSheets(Actor actor)
            => Sheets.GetActorSheets(actor);
        
        /// <summary>
        ///   <para>查询拥有指定组件的全部行动者</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<Actor> QueryActorsWithComponent<T>()
            where T : struct, IComponent
            => Actors.QueryActorsWithComponent<T>();
        
        /// <summary>
        ///   <para>查询同时拥有指定多组件的全部行动者</para>
        /// </summary>
        /// <param name="componentTypes">组件类型列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<Actor> QueryActorsWithComponents(params Type[] componentTypes)
            => Actors.QueryActorsWithComponents(componentTypes);

        /// <summary>
        ///   <para>设置网络传输实现</para>
        /// </summary>
        /// <param name="transport">网络同步传输接口</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNetworkTransport(INetworkSyncTransport transport)
            => m_NetworkSync?.SetTransport(transport);
        
        /// <summary>
        ///   <para>设置网络同步组件序列化器</para>
        /// </summary>
        /// <param name="serializer">网络同步组件序列化器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNetworkSerializer(INetworkComponentSerializer serializer)
            => m_NetworkSync?.SetSerializer(serializer);
        
        /// <summary>
        ///   <para>设置网络同步头部格式化器</para>
        /// </summary>
        /// <param name="formatter">网络同步头部格式化器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNetworkHeaderFormatter(INetworkPacketHeaderFormatter formatter)
            => m_NetworkSync?.SetHeaderFormatter(formatter);
        
        /// <summary>
        ///   <para>取消注册网络同步组件</para>
        /// </summary>
        /// <param name="actor">行动者</param>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnregisterReplicatedComponent<T>(Actor actor)
            where T : struct, IComponent
            => m_NetworkSync?.UnregisterReplicatedComponent<T>(actor);
        
        /// <summary>
        ///   <para>手动标记指定组件为脏数据</para>
        /// </summary>
        /// <param name="actor">行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkComponentDirty<T>(Actor actor)
            where T : struct, IComponent
            => m_NetworkSync?.MarkComponentDirty<T>(actor);
        
        /// <summary>
        ///   <para>执行世界逻辑帧（按<see cref="TickGroup"/>分组驱动所有能力）</para>
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        [Obsolete("Please use Tick(float deltaTime, TickGroup tickGroup) instead.")]
        internal void Tick(float deltaTime)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(World));
            float scaledDelta = deltaTime * m_TimeScale;
            m_NetworkSync?.BeforeTick(scaledDelta, TickGroup.Gameplay);
            Capabilities?.Update(scaledDelta);
            m_NetworkSync?.AfterTick(scaledDelta, TickGroup.Gameplay);
        }
        
        /// <summary>
        ///   <para>执行指定<see cref="TickGroup"/>分组世界逻辑帧</para>
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        /// <param name="tickGroup">更新分组</param>
        internal void Tick(float deltaTime, TickGroup tickGroup)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(World));
            float scaledDelta = deltaTime * m_TimeScale;
            m_NetworkSync?.BeforeTick(scaledDelta, tickGroup);
            Capabilities?.Update(scaledDelta, tickGroup);
            m_NetworkSync?.AfterTick(scaledDelta, tickGroup);
        }
        
        /// <summary>
        ///   <para>清理世界</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(World));
            Actors?.Clear();
            Capabilities?.Clear();
        }
        
        /// <summary>
        ///   <para>释放世界及其资源</para>
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            try
            {
                m_NetworkSync?.Dispose();
                Capabilities?.Dispose();
                Actors?.Dispose();
            }
            finally
            {
                IsDisposed = true;
            }

            GC.SuppressFinalize(this);
        }
        
        public override string ToString() => $"{nameof(World)}: {Name}";
    }
    
    #endregion
}