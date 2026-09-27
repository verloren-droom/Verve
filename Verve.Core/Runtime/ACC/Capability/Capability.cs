// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>能力基类；由 <see cref="World"/> 创建、调度和释放。</para>
    /// </summary>
    [Serializable, DebuggerDisplay("{ToString}")]
    public abstract class Capability
    {
        /// <summary>
        ///   <para>能力的默认更新阶段。</para>
        /// </summary>
        private const TickGroup DEFAULT_TICK_GROUP = TickGroup.Gameplay;

        /// <summary>
        ///   <para>所属行动者句柄。</para>
        /// </summary>
        private Actor m_OwnerActor = Actor.none;
        /// <summary>
        ///   <para>所属世界。</para>
        /// </summary>
        private World m_OwnerWorld;
        /// <summary>
        ///   <para>能力标签的池化存储；元素类型为 <see cref="TagId"/>。</para>
        /// </summary>
        private TagId[] m_TagBuffer;
        /// <summary>
        ///   <para>当前能力登记的标签阻塞；用于自动释放所有权。</para>
        /// </summary>
        private TagId[] m_BlockedTagBuffer;
        /// <summary>
        ///   <para>当前标签数量。</para>
        /// </summary>
        private int m_TagCount;
        /// <summary>
        ///   <para>当前标签阻塞数量。</para>
        /// </summary>
        private int m_BlockedTagCount;
        /// <summary>
        ///   <para>激活所需组件的掩码。</para>
        /// </summary>
        private ComponentMask m_RequiredComponents;
        /// <summary>
        ///   <para>阻止激活的组件掩码。</para>
        /// </summary>
        private ComponentMask m_BlockedComponents;
        /// <summary>
        ///   <para>防止释放回调重入。</para>
        /// </summary>
        private bool m_IsReleasing;
        /// <summary>
        ///   <para>是否正在执行初始设置。</para>
        /// </summary>
        private bool m_IsSettingUp;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        ///   <para>上一次激活检查是否被阻塞。</para>
        /// </summary>
        private bool m_DebugActivationBlocked;
#endif
        /// <summary>
        ///   <para>是否已请求移除；阻止本帧继续调用。</para>
        /// </summary>
        internal bool IsRemovalPending { get; set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        ///   <para>更新调试用的激活阻塞状态。</para>
        /// </summary>
        /// <param name="blocked">是否被阻塞。</param>
        /// <returns>状态是否发生变化。</returns>
        internal bool SetDebugActivationBlocked(bool blocked)
        {
            if (m_DebugActivationBlocked == blocked)
                return false;
            m_DebugActivationBlocked = blocked;
            return true;
        }
#endif

        /// <summary>
        ///   <para>能力拥有者。</para>
        /// </summary>
        public Actor OwnerActor { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_OwnerActor; }
        /// <summary>
        ///   <para>能力所在世界。</para>
        /// </summary>
        public World OwnerWorld { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_OwnerWorld; }
        /// <summary>
        ///   <para>是否激活。</para>
        /// </summary>
        public bool IsActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] internal set;
        }
        /// <summary>
        ///   <para>更新分组；决定能力执行阶段。</para>
        /// </summary>
        public TickGroup TickGroup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] private set;
        } = DEFAULT_TICK_GROUP;
        /// <summary>
        ///   <para>组内执行顺序；数值越小越先执行。</para>
        /// </summary>
        public int TickOrder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] private set;
        }
        /// <summary>
        ///   <para>是否已释放。</para>
        /// </summary>
        public bool IsReleased
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] private set;
        }
        /// <summary>
        ///   <para>标签集合；用于能力间阻塞和解锁控制。</para>
        /// </summary>
        public ReadOnlySpan<TagId> Tags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_TagCount <= 0
                ? ReadOnlySpan<TagId>.Empty
                : new ReadOnlySpan<TagId>(m_TagBuffer, 0, m_TagCount);
        }
        /// <summary>
        ///   <para>所需组件掩码；拥有这些组件方可激活。</para>
        /// </summary>
        public ref readonly ComponentMask RequiredComponents => ref m_RequiredComponents;
        /// <summary>
        ///   <para>阻塞组件掩码；拥有这些组件则阻止激活。</para>
        /// </summary>
        public ref readonly ComponentMask BlockedComponents => ref m_BlockedComponents;
        /// <summary>
        ///   <para>能力类型标识符；仅用于进程内索引。</para>
        /// </summary>
        public CapabilityTypeId TypeId => CapabilityTypeRegistry.GetTypeId(GetType());

        /// <summary>
        ///   <para>设置能力实例；绑定所属 <see cref="Actor"/> 和 <see cref="World"/>。</para>
        /// </summary>
        /// <param name="actor">能力所属行动者。</param>
        /// <param name="world">能力所属的 <see cref="World"/>。</param>
        internal void Setup(Actor actor, World world)
        {
            m_OwnerActor = actor;
            m_OwnerWorld = world;
            m_IsSettingUp = true;
            try { OnSetup(); }
            finally { m_IsSettingUp = false; }
        }

        /// <summary>
        ///   <para>设置；能力添加后执行一次。</para>
        /// </summary>
        protected virtual void OnSetup() { }

        /// <summary>
        ///   <para>判断是否应该激活；由调度器调用。</para>
        /// </summary>
        protected internal virtual bool ShouldActivate()
        {
            if (m_RequiredComponents.IsEmpty && m_BlockedComponents.IsEmpty) return true;
            var actorMask = m_OwnerWorld.Actors.GetComponentMask(m_OwnerActor);
            return actorMask.ContainsAll(m_RequiredComponents) &&
                   actorMask.ContainsNone(m_BlockedComponents);
        }

        /// <summary>
        ///   <para>激活回调；能力变为激活状态时调用。</para>
        /// </summary>
        protected internal virtual void OnActivated() { }

        /// <summary>
        ///   <para>激活 Tick；在世界所属线程按阶段和顺序调用。</para>
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        protected internal virtual void TickActive(in float deltaTime) { }

        /// <summary>
        ///   <para>判断是否应该失活；由调度器调用。</para>
        /// </summary>
        protected internal virtual bool ShouldDeactivate()
        {
            if (m_RequiredComponents.IsEmpty && m_BlockedComponents.IsEmpty) return false;
            var actorMask = m_OwnerWorld.Actors.GetComponentMask(m_OwnerActor);
            return !actorMask.ContainsAll(m_RequiredComponents) ||
                   actorMask.ContainsAny(m_BlockedComponents);
        }

        /// <summary>
        ///   <para>失活回调；能力离开激活状态时调用。</para>
        /// </summary>
        protected internal virtual void OnDeactivated() { }

        /// <summary>
        ///   <para>由所属的 <see cref="CapabilityManager"/> 释放能力；外部不能直接管理能力生命周期。</para>
        /// </summary>
        internal void Release()
        {
            if (IsReleased || m_IsReleasing) return;
            m_IsReleasing = true;
            IsRemovalPending = true;
            List<Exception> errors = null;
            try
            {
                try { Deactivate(); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                try { Dispose(true); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            }
            finally
            {
                ReleaseBlockedTags(ref errors);
                m_RequiredComponents.Clear();
                m_BlockedComponents.Clear();
                ReturnTagBuffers();
                m_OwnerActor = Actor.none;
                m_OwnerWorld = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_DebugActivationBlocked = false;
#endif
                IsReleased = true;
                m_IsReleasing = false;
            }
            if (errors != null)
                throw new AggregateException($"Releasing {GetType().Name} failed.", errors);
        }

        /// <summary>
        ///   <para>结束一次激活并自动解除当前能力持有的标签阻塞。</para>
        /// </summary>
        internal void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_OwnerWorld?.DebugTrace.Record(m_OwnerActor, this, CapabilityDebugEventKind.Deactivated);
#endif
            List<Exception> errors = null;
            try { OnDeactivated(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            finally { ReleaseBlockedTags(ref errors); }
            if (errors != null)
                throw new AggregateException($"Deactivating {GetType().Name} failed.", errors);
        }

        /// <summary>
        ///   <para>解除当前能力登记的全部标签阻塞。</para>
        /// </summary>
        /// <param name="errors">接收解除失败异常的列表。</param>
        private void ReleaseBlockedTags(ref List<Exception> errors)
        {
            if (m_BlockedTagCount == 0)
                return;

            var world = m_OwnerWorld;
            var actor = m_OwnerActor;
            for (int i = m_BlockedTagCount - 1; i >= 0; i--)
            {
                try
                {
                    if (world != null)
                        world.Capabilities.UnblockTag(actor, m_BlockedTagBuffer[i], this);
                }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            }
            m_BlockedTagCount = 0;
        }

        /// <summary>
        ///   <para>归还能力持有的标签数组。</para>
        /// </summary>
        private void ReturnTagBuffers()
        {
            if (m_TagBuffer != null)
            {
                ArrayPool<TagId>.Shared.Return(m_TagBuffer, false);
                m_TagBuffer = null;
            }
            if (m_BlockedTagBuffer != null)
            {
                ArrayPool<TagId>.Shared.Return(m_BlockedTagBuffer, false);
                m_BlockedTagBuffer = null;
            }
            m_TagCount = 0;
            m_BlockedTagCount = 0;
        }

        /// <summary>
        ///   <para>释放资源；执行失活回调并清理能力状态。</para>
        /// </summary>
        /// <param name="disposing">是否由显式释放触发。</param>
        protected virtual void Dispose(bool disposing) { }

        /// <summary>
        ///   <para>依赖组件；添加激活所需的 <see cref="IComponent"/> 类型。</para>
        /// </summary>
        /// <typeparam name="T">所需组件类型。</typeparam>
        protected void Require<T>() where T : struct, IComponent
            => m_RequiredComponents.Add(ComponentTypeRegistry<T>.id);

        /// <summary>
        ///   <para>阻塞组件；添加会阻止能力激活的 <see cref="IComponent"/> 类型。</para>
        /// </summary>
        /// <typeparam name="T">阻止激活的组件类型。</typeparam>
        protected void Block<T>() where T : struct, IComponent
            => m_BlockedComponents.Add(ComponentTypeRegistry<T>.id);

        /// <summary>
        ///   <para>依赖组件；添加激活所需的 <see cref="IComponent"/> 类型。</para>
        /// </summary>
        /// <param name="componentType">所需组件类型。</param>
        protected void Require(Type componentType)
            => m_RequiredComponents.Add(ComponentTypeRegistry.GetTypeId(componentType));

        /// <summary>
        ///   <para>阻塞组件；添加会阻止能力激活的 <see cref="IComponent"/> 类型。</para>
        /// </summary>
        /// <param name="componentType">阻止激活的组件类型。</param>
        protected void Block(Type componentType)
            => m_BlockedComponents.Add(ComponentTypeRegistry.GetTypeId(componentType));

        /// <summary>
        ///   <para>添加标签；将 <see cref="TagId"/> 加入能力标签集合。</para>
        /// </summary>
        /// <param name="tagId">要添加的标签标识。</param>
        protected void AddTag(TagId tagId)
        {
            EnsureTagCapacity(ref m_TagBuffer, m_TagCount, m_TagCount + 1);
            m_TagBuffer[m_TagCount++] = tagId;
        }

        /// <summary>
        ///   <para>批量添加标签；将多个 <see cref="TagId"/> 加入能力标签集合。</para>
        /// </summary>
        /// <param name="tags">要添加的标签标识。</param>
        protected void AddTags(ReadOnlySpan<TagId> tags)
        {
            if (tags.Length == 0) return;
            EnsureTagCapacity(ref m_TagBuffer, m_TagCount, m_TagCount + tags.Length);
            tags.CopyTo(new Span<TagId>(m_TagBuffer, m_TagCount, tags.Length));
            m_TagCount += tags.Length;
        }

        /// <summary>
        ///   <para>判断是否拥有标签；查询能力标签集合。</para>
        /// </summary>
        /// <param name="tagId">待检查的标签标识。</param>
        protected bool HasTag(TagId tagId)
        {
            for (int i = 0; i < m_TagCount; i++)
                if (m_TagBuffer[i] == tagId) return true;
            return false;
        }

        /// <summary>
        ///   <para>添加标签；按名称注册并加入能力标签集合。</para>
        /// </summary>
        /// <param name="tagName">要添加的标签名称。</param>
        protected void AddTag(string tagName)
        {
            AddTag(TagRegistry.GetTagId(tagName));
        }

        /// <summary>
        ///   <para>添加多个标签；按名称注册并加入能力标签集合。</para>
        /// </summary>
        /// <param name="tagNames">要添加的标签名称。</param>
        protected void AddTags(params string[] tagNames)
        {
            if (tagNames == null) throw new ArgumentNullException(nameof(tagNames));
            EnsureTagCapacity(ref m_TagBuffer, m_TagCount, m_TagCount + tagNames.Length);
            for (int i = 0; i < tagNames.Length; i++)
                m_TagBuffer[m_TagCount++] = TagRegistry.GetTagId(tagNames[i]);
        }

        /// <summary>
        ///   <para>检查标签是否被阻塞；查询所属 <see cref="World"/> 的阻塞状态。</para>
        /// </summary>
        /// <param name="tagName">待检查的标签名称。</param>
        protected bool IsTagBlocked(string tagName) => IsTagBlocked(TagRegistry.GetTagId(tagName));

        /// <summary>
        ///   <para>阻塞指定标签的能力；以当前能力作为发起者。</para>
        /// </summary>
        /// <param name="tagName">要阻塞的标签名称。</param>
        protected void BlockCapabilitiesWithTag(string tagName)
        {
            BlockCapabilitiesWithTag(TagRegistry.GetTagId(tagName));
        }

        /// <summary>
        ///   <para>解除阻塞指定标签的能力；以当前能力作为发起者。</para>
        /// </summary>
        /// <param name="tagName">要解除阻塞的标签名称。</param>
        protected void UnblockCapabilitiesWithTag(string tagName)
        {
            UnblockCapabilitiesWithTag(TagRegistry.GetTagId(tagName));
        }

        /// <summary>
        ///   <para>清空标签；移除当前能力记录的全部标签。</para>
        /// </summary>
        protected void ClearTags()
        {
            m_TagCount = 0;
        }

        /// <summary>
        ///   <para>阻塞标签；在所属 <see cref="World"/> 中登记当前能力。</para>
        /// </summary>
        /// <param name="tagId">要阻塞的标签标识。</param>
        protected void BlockCapabilitiesWithTag(TagId tagId)
        {
            if (IsReleased || m_IsReleasing) throw new ObjectDisposedException(GetType().Name);
            EnsureTagCapacity(ref m_BlockedTagBuffer, m_BlockedTagCount, m_BlockedTagCount + 1);
            m_OwnerWorld.Capabilities.BlockTag(m_OwnerActor, tagId, this);
            m_BlockedTagBuffer[m_BlockedTagCount++] = tagId;
        }

        /// <summary>
        ///   <para>解除阻塞标签；在所属 <see cref="World"/> 中移除当前能力登记。</para>
        /// </summary>
        /// <param name="tagId">要解除阻塞的标签标识。</param>
        protected void UnblockCapabilitiesWithTag(TagId tagId)
        {
            m_OwnerWorld.Capabilities.UnblockTag(m_OwnerActor, tagId, this);
            RemoveBlockedTag(tagId);
        }

        /// <summary>
        ///   <para>批量阻塞标签；在所属 <see cref="World"/> 中登记多个标签。</para>
        /// </summary>
        /// <param name="tags">要阻塞的标签标识。</param>
        protected void BlockCapabilitiesWithTags(ReadOnlySpan<TagId> tags)
        {
            for (int i = 0; i < tags.Length; i++)
                BlockCapabilitiesWithTag(tags[i]);
        }

        /// <summary>
        ///   <para>批量解除阻塞标签；在所属 <see cref="World"/> 中移除多个登记。</para>
        /// </summary>
        /// <param name="tags">要解除阻塞的标签标识。</param>
        protected void UnblockCapabilitiesWithTags(ReadOnlySpan<TagId> tags)
        {
            for (int i = 0; i < tags.Length; i++)
                UnblockCapabilitiesWithTag(tags[i]);
        }

        /// <summary>
        ///   <para>判断标签是否被阻塞；查询所属 <see cref="World"/> 的阻塞状态。</para>
        /// </summary>
        /// <param name="tagId">待检查的标签标识。</param>
        protected bool IsTagBlocked(TagId tagId)
        {
            return m_OwnerWorld.Capabilities.IsTagBlocked(m_OwnerActor, tagId);
        }

        /// <summary>
        ///   <para>判断标签是否被阻塞；任一标签阻塞时返回 <see langword="true"/>。</para>
        /// </summary>
        /// <param name="tags">待检查的标签标识。</param>
        protected bool AnyTagBlocked(ReadOnlySpan<TagId> tags)
        {
            for (int i = 0; i < tags.Length; i++)
                if (m_OwnerWorld.Capabilities.IsTagBlocked(m_OwnerActor, tags[i]))
                    return true;
            return false;
        }

        /// <summary>
        ///   <para>设置执行顺序；仅在 <see cref="OnSetup"/> 中设置调度参数。</para>
        /// </summary>
        /// <param name="group">更新阶段。</param>
        /// <param name="order">阶段内执行顺序。</param>
        protected void SetTick(TickGroup group, int order)
        {
            if (!m_IsSettingUp)
                throw new InvalidOperationException("Tick settings can only be assigned during OnSetup.");
            TickGroup = group;
            TickOrder = order;
        }

        public override string ToString() => $"{GetType().Name} (Owner: {m_OwnerActor}, Active: {IsActive}, Group: {TickGroup}, Order: {TickOrder})";

        /// <summary>
        ///   <para>扩展池化标签数组。</para>
        /// </summary>
        /// <param name="buffer">目标数组。</param>
        /// <param name="count">有效元素数量。</param>
        /// <param name="requiredCount">所需容量。</param>
        private static void EnsureTagCapacity(ref TagId[] buffer, int count, int requiredCount)
        {
            if (buffer != null && requiredCount <= buffer.Length) return;
            var next = ArrayPool<TagId>.Shared.Rent(Math.Max(requiredCount, buffer?.Length * 2 ?? 4));
            if (buffer != null)
            {
                Array.Copy(buffer, next, count);
                ArrayPool<TagId>.Shared.Return(buffer);
            }
            buffer = next;
        }

        /// <summary>
        ///   <para>移除一次指定的标签阻塞登记。</para>
        /// </summary>
        /// <param name="tagId">待移除的标签标识。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveBlockedTag(TagId tagId)
        {
            for (int i = m_BlockedTagCount - 1; i >= 0; i--)
            {
                if (m_BlockedTagBuffer[i] != tagId) continue;
                int last = --m_BlockedTagCount;
                if (i != last)
                    m_BlockedTagBuffer[i] = m_BlockedTagBuffer[last];
                return;
            }
        }
    }
}
