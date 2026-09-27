// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>表单管理器；由 <see cref="CapabilityManager"/> 独占拥有。</para>
    /// </summary>
    [Serializable]
    public sealed class SheetManager
    {
        /// <summary>
        ///   <para>所属世界。</para>
        /// </summary>
        private readonly World m_World;
        /// <summary>
        ///   <para>按行动者保存的表单实例。</para>
        /// </summary>
        private readonly Dictionary<Actor, List<SheetInstance>> m_ActorSheets = new(32);
        /// <summary>
        ///   <para>按行动者和组件类型统计表单所有权；最后一个所有者释放时才移除组件。</para>
        /// </summary>
        private readonly Dictionary<Actor, Dictionary<Type, ComponentOwnership>> m_ComponentOwnership = new(32);
        /// <summary>
        ///   <para>保护表单实例映射。</para>
        /// </summary>
        private readonly object m_Lock = new();

        /// <summary>
        ///   <para>表单组件所有权记录。</para>
        /// </summary>
        private struct ComponentOwnership
        {
            /// <summary>
            ///   <para>当前声明数量。</para>
            /// </summary>
            internal int Count;
            /// <summary>
            ///   <para>是否至少由一个表单创建。</para>
            /// </summary>
            internal bool CreatedBySheet;
        }

        /// <summary>
        ///   <para>创建指定世界的表单管理器。</para>
        /// </summary>
        /// <param name="world">所属世界。</param>
        internal SheetManager(World world)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>
        ///   <para>应用表单并登记其资源所有权。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="sheet">待应用表单。</param>
        /// <param name="mode">应用模式。</param>
        public SheetInstance ApplySheet(Actor actor, CapabilitySheet sheet, CapabilitySheetApplyMode mode = CapabilitySheetApplyMode.All)
        {
            m_World.ValidateStructuralChange(true);
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            var instance = sheet.ApplyTo(actor, m_World, mode);
            try
            {
                instance.SetOwner(this);
                lock (m_Lock)
                {
                    RegisterComponentOwnershipNoLock(actor, instance);
                    if (!m_ActorSheets.TryGetValue(actor, out var instances))
                        m_ActorSheets.Add(actor, instances = new List<SheetInstance>());
                    instances.Add(instance);
                }
                return instance;
            }
            catch (Exception registrationException)
            {
                lock (m_Lock)
                {
                    UnregisterComponentOwnershipNoLock(actor, instance);
                    if (m_ActorSheets.TryGetValue(actor, out var tracked) && tracked.Count == 0)
                        m_ActorSheets.Remove(actor);
                }

                try
                {
                    instance.SetOwner(null);
                    instance.RemoveFromActor();
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException("Registering the sheet failed and rollback was incomplete.", registrationException, rollbackException);
                }
                throw;
            }
        }

        /// <summary>
        ///   <para>移除指定行动者上的表单实例。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="instance">待移除实例。</param>
        public bool RemoveSheet(Actor actor, SheetInstance instance)
        {
            if (instance == null) return false;
            lock (m_Lock)
            {
                if (!m_ActorSheets.TryGetValue(actor, out var instances))
                    return false;
                if (!instances.Contains(instance))
                    return false;
            }
            instance.RemoveFromActor();
            return true;
        }

        /// <summary>
        ///   <para>移除行动者上的全部表单实例。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        public void RemoveAllSheets(Actor actor)
        {
            SheetInstance[] instances;
            lock (m_Lock)
            {
                if (!m_ActorSheets.TryGetValue(actor, out var tracked))
                    return;
                instances = tracked.ToArray();
            }

            List<Exception> errors = null;
            for (var i = instances.Length - 1; i >= 0; i--)
            {
                try { instances[i].RemoveFromActor(); }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
            }
            if (errors != null) throw new AggregateException("One or more sheets could not be removed.", errors);
        }

        /// <summary>
        ///   <para>获取行动者当前登记的表单实例快照。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        public IReadOnlyList<SheetInstance> GetActorSheets(Actor actor)
        {
            lock (m_Lock)
            {
                return m_ActorSheets.TryGetValue(actor, out var instances)
                    ? new List<SheetInstance>(instances)
                    : (IReadOnlyList<SheetInstance>)Array.Empty<SheetInstance>();
            }
        }

        /// <summary>
        ///   <para>移除所有表单实例并释放其拥有的资源。</para>
        /// </summary>
        public void Clear()
        {
            SheetInstance[] snapshot;
            lock (m_Lock)
            {
                var count = 0;
                foreach (var instances in m_ActorSheets.Values)
                    count += instances.Count;
                snapshot = new SheetInstance[count];
                var index = 0;
                foreach (var instances in m_ActorSheets.Values)
                    for (var i = 0; i < instances.Count; i++)
                        snapshot[index++] = instances[i];
            }

            List<Exception> errors = null;
            for (var i = snapshot.Length - 1; i >= 0; i--)
            {
                try { snapshot[i].RemoveFromActor(); }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
            }
            if (errors != null) throw new AggregateException("One or more sheets could not be cleared.", errors);
        }

        /// <summary>
        ///   <para>实例完成释放后注销管理器登记。</para>
        /// </summary>
        /// <param name="instance">已移除的表单实例。</param>
        internal void OnInstanceRemoved(SheetInstance instance)
        {
            lock (m_Lock)
            {
                var actor = instance.OwnerActor;
                if (!m_ActorSheets.TryGetValue(actor, out var instances) || !instances.Remove(instance)) return;
                if (instances.Count == 0)
                    m_ActorSheets.Remove(actor);
            }
        }

        /// <summary>
        ///   <para>释放一个表单组件所有权；仅最后一个由表单创建的所有者会移除组件。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="entry">组件声明。</param>
        internal void ReleaseComponentOwnership(Actor actor, SheetInstance.ComponentEntry entry)
        {
            ComponentOwnership ownership;
            lock (m_Lock)
            {
                if (!m_ComponentOwnership.TryGetValue(actor, out var ownershipByType) ||
                    !ownershipByType.TryGetValue(entry.Type, out ownership) || ownership.Count <= 0)
                    throw new InvalidOperationException("The sheet component ownership record is missing.");

                if (ownership.Count > 1)
                {
                    ownership.Count--;
                    ownershipByType[entry.Type] = ownership;
                    return;
                }

                if (!ownership.CreatedBySheet || !m_World.IsActorAlive(actor))
                {
                    ownershipByType.Remove(entry.Type);
                    if (ownershipByType.Count == 0) m_ComponentOwnership.Remove(actor);
                    return;
                }
            }

            // 组件移除可能抛出异常；保留登记以便调用方重试，不静默丢失所有权。
            CapabilitySheet.GetRemoveComponentFunc(entry.Type)(m_World, actor);

            lock (m_Lock)
            {
                if (m_ComponentOwnership.TryGetValue(actor, out var ownershipByType) &&
                    ownershipByType.TryGetValue(entry.Type, out ownership) && ownership.Count == 1)
                {
                    ownershipByType.Remove(entry.Type);
                    if (ownershipByType.Count == 0) m_ComponentOwnership.Remove(actor);
                }
            }
        }

        /// <summary>
        ///   <para>登记实例及其子实例的组件声明；调用方已持有锁。</para>
        /// </summary>
        private void RegisterComponentOwnershipNoLock(Actor actor, SheetInstance instance)
        {
            if (!m_ComponentOwnership.TryGetValue(actor, out var ownershipByType))
                m_ComponentOwnership.Add(actor, ownershipByType = new Dictionary<Type, ComponentOwnership>());

            for (var i = 0; i < instance.Components.Count; i++)
            {
                var entry = instance.Components[i];
                ownershipByType.TryGetValue(entry.Type, out var ownership);
                ownership.Count++;
                ownership.CreatedBySheet |= entry.Created;
                ownershipByType[entry.Type] = ownership;
            }

            for (var i = 0; i < instance.SubInstances.Count; i++)
                RegisterComponentOwnershipNoLock(actor, instance.SubInstances[i]);

            if (ownershipByType.Count == 0)
                m_ComponentOwnership.Remove(actor);
        }

        /// <summary>
        ///   <para>撤销实例及其子实例的组件声明；调用方已持有锁。</para>
        /// </summary>
        private void UnregisterComponentOwnershipNoLock(Actor actor, SheetInstance instance)
        {
            if (!m_ComponentOwnership.TryGetValue(actor, out var ownershipByType)) return;
            for (var i = 0; i < instance.Components.Count; i++)
            {
                var type = instance.Components[i].Type;
                if (!ownershipByType.TryGetValue(type, out var ownership)) continue;
                if (--ownership.Count <= 0) ownershipByType.Remove(type);
                else ownershipByType[type] = ownership;
            }
            for (var i = 0; i < instance.SubInstances.Count; i++)
                UnregisterComponentOwnershipNoLock(actor, instance.SubInstances[i]);
            if (ownershipByType.Count == 0) m_ComponentOwnership.Remove(actor);
        }
    }
}