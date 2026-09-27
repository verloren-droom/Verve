// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>能力表单实例；记录一次应用产生的所有权。</para>
    /// </summary>
    [Serializable]
    public sealed class SheetInstance : IDisposable
    {
        /// <summary>
        ///   <para>组件声明；记录本次表单是否创建了组件。</para>
        /// </summary>
        internal readonly struct ComponentEntry
        {
            /// <summary>
            ///   <para>组件类型。</para>
            /// </summary>
            internal readonly Type Type;
            /// <summary>
            ///   <para>是否由表单创建。</para>
            /// </summary>
            internal readonly bool Created;

            /// <summary>
            ///   <para>创建组件声明。</para>
            /// </summary>
            /// <param name="type">组件类型。</param>
            /// <param name="created">是否由表单创建。</param>
            internal ComponentEntry(Type type, bool created)
            {
                Type = type;
                Created = created;
            }
        }

        /// <summary>
        ///   <para>应用目标行动者。</para>
        /// </summary>
        private readonly Actor m_Actor;
        /// <summary>
        ///   <para>所属世界。</para>
        /// </summary>
        private readonly World m_World;
        /// <summary>
        ///   <para>由此实例创建的能力。</para>
        /// </summary>
        private readonly List<Capability> m_Capabilities = new(16);
        /// <summary>
        ///   <para>由此实例声明的组件类型。</para>
        /// </summary>
        private readonly List<ComponentEntry> m_Components = new(16);
        /// <summary>
        ///   <para>由此实例创建的子表单实例。</para>
        /// </summary>
        private readonly List<SheetInstance> m_SubInstances = new(8);

        /// <summary>
        ///   <para>清理重入标记。</para>
        /// </summary>
        private bool m_IsRemoving;
        /// <summary>
        ///   <para>拥有此顶层实例的表单管理器。</para>
        /// </summary>
        private SheetManager m_Owner;

        /// <summary>
        ///   <para>是否已移除全部表单资源；线程校验失败不会改变状态。</para>
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <summary>
        ///   <para>此实例所属的行动者。</para>
        /// </summary>
        internal Actor OwnerActor => m_Actor;
        /// <summary>
        ///   <para>当前实例声明的组件。</para>
        /// </summary>
        internal IReadOnlyList<ComponentEntry> Components => m_Components;
        /// <summary>
        ///   <para>当前实例的子表单实例。</para>
        /// </summary>
        internal IReadOnlyList<SheetInstance> SubInstances => m_SubInstances;

        /// <summary>
        ///   <para>创建指定行动者的表单实例。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="world">所属世界。</param>
        internal SheetInstance(Actor actor, World world)
        {
            m_Actor = actor;
            m_World = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <inheritdoc />
        public void Dispose() => RemoveFromActor();

        /// <summary>
        ///   <para>设置拥有此顶层实例的管理器。</para>
        /// </summary>
        /// <param name="owner">表单管理器。</param>
        internal void SetOwner(SheetManager owner)
        {
            m_Owner = owner;
            for (var i = 0; i < m_SubInstances.Count; i++)
                m_SubInstances[i].SetOwner(owner);
        }

        /// <summary>
        ///   <para>记录由当前表单创建的子实例。</para>
        /// </summary>
        /// <param name="subInstance">子表单实例。</param>
        internal void AddSubInstance(SheetInstance subInstance) => m_SubInstances.Add(subInstance);

        /// <summary>
        ///   <para>记录由当前表单创建的能力。</para>
        /// </summary>
        /// <param name="capability">能力实例。</param>
        internal void AddCapability(Capability capability) => m_Capabilities.Add(capability);

        /// <summary>
        ///   <para>记录当前表单声明的组件。</para>
        /// </summary>
        /// <param name="componentType">组件类型。</param>
        internal void AddComponent(Type componentType, bool created)
            => m_Components.Add(new ComponentEntry(componentType, created));

        /// <summary>
        ///   <para>移除当前实例拥有的资源；重复调用不会重复释放。</para>
        /// </summary>
        public void RemoveFromActor()
        {
            if (IsDisposed) return;
            m_World.ValidateStructuralChange();
            if (m_IsRemoving) throw new InvalidOperationException("A sheet cannot be removed during its own cleanup.");
            if (!m_World.IsActorAlive(m_Actor))
            {
                for (var i = m_SubInstances.Count - 1; i >= 0; i--)
                {
                    m_SubInstances[i].RemoveFromActor();
                    m_SubInstances.RemoveAt(i);
                }
                m_Capabilities.Clear();
                if (m_Owner != null)
                    for (var i = m_Components.Count - 1; i >= 0; i--)
                        m_Owner.ReleaseComponentOwnership(m_Actor, m_Components[i]);
                m_Components.Clear();
                IsDisposed = true;
                m_Owner?.OnInstanceRemoved(this);
                return;
            }

            m_IsRemoving = true;
            try
            {
                List<Exception> errors = null;
                for (var i = m_SubInstances.Count - 1; i >= 0; i--)
                {
                    var instance = m_SubInstances[i];
                    try { instance.RemoveFromActor(); }
                    catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                    finally
                    {
                        if (instance.IsDisposed) m_SubInstances.RemoveAt(i);
                    }
                }

                for (var i = m_Capabilities.Count - 1; i >= 0; i--)
                {
                    var capability = m_Capabilities[i];
                    try
                    {
                        m_World.Capabilities.RemoveCapability(m_Actor, capability);
                        m_Capabilities.RemoveAt(i);
                    }
                    catch (Exception exception)
                    {
                        // 回调失败仍可能完成释放，不保留已失效的所有权记录。
                        if (capability.IsReleased) m_Capabilities.RemoveAt(i);
                        (errors ??= new List<Exception>()).Add(exception);
                    }
                }

                for (var i = m_Components.Count - 1; i >= 0; i--)
                {
                    var component = m_Components[i];
                    try
                    {
                        if (m_Owner != null)
                            m_Owner.ReleaseComponentOwnership(m_Actor, component);
                        else if (component.Created)
                            CapabilitySheet.GetRemoveComponentFunc(component.Type)(m_World, m_Actor);
                        m_Components.RemoveAt(i);
                    }
                    catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                }

                IsDisposed = m_SubInstances.Count == 0 && m_Capabilities.Count == 0 && m_Components.Count == 0;
                if (IsDisposed) m_Owner?.OnInstanceRemoved(this);
                if (errors != null)
                    throw new AggregateException("One or more capability sheet resources could not be removed.", errors);
            }
            finally { m_IsRemoving = false; }
        }
    }
}
