// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>能力调度队列；在世界线程按阶段、顺序和注册次序执行。</para>
    /// </summary>
    internal sealed class ActionQueue
    {
        /// <summary>
        ///   <para>调度条目；保存注册时的执行顺序。</para>
        /// </summary>
        private readonly struct Entry
        {
            /// <summary>
            ///   <para>能力实例。</para>
            /// </summary>
            public readonly Capability capability;
            /// <summary>
            ///   <para>组内顺序。</para>
            /// </summary>
            public readonly int order;
            /// <summary>
            ///   <para>注册次序；保证相同顺序的能力稳定排序。</para>
            /// </summary>
            public readonly long sequence;

            /// <summary>
            ///   <para>创建调度条目。</para>
            /// </summary>
            /// <param name="capability">能力实例。</param>
            /// <param name="order">组内顺序。</param>
            /// <param name="sequence">注册次序。</param>
            public Entry(Capability capability, int order, long sequence)
                => (this.capability, this.order, this.sequence) = (capability, order, sequence);
        }

        /// <summary>
        ///   <para>调度组；增删后批量压缩并排序。</para>
        /// </summary>
        private sealed class Group
        {
            /// <summary>
            ///   <para>执行条目。</para>
            /// </summary>
            public readonly List<Entry> entries = new(64);
            /// <summary>
            ///   <para>组标识。</para>
            /// </summary>
            public readonly int id;
            /// <summary>
            ///   <para>是否需要重建索引。</para>
            /// </summary>
            public bool dirty;

            /// <summary>
            ///   <para>创建调度组。</para>
            /// </summary>
            /// <param name="id">组标识。</param>
            public Group(int id) => this.id = id;
        }

        /// <summary>
        ///   <para>按阶段排列的组。</para>
        /// </summary>
        private readonly List<Group> m_Groups = new();
        /// <summary>
        ///   <para>组标识索引。</para>
        /// </summary>
        private readonly Dictionary<int, Group> m_GroupMap = new();
        /// <summary>
        ///   <para>能力位置索引；移除时直接置空，无需移动整个列表。</para>
        /// </summary>
        private readonly Dictionary<Capability, (Group group, int index)> m_Positions = new(256, ReferenceEqualityComparer<Capability>.Instance);
        /// <summary>
        ///   <para>递增注册次序。</para>
        /// </summary>
        private long m_Sequence;

        /// <summary>
        ///   <para>登记能力；下次更新前统一排序。</para>
        /// </summary>
        /// <param name="capability">能力实例。</param>
        /// <param name="tickGroup">执行阶段。</param>
        /// <param name="tickOrder">组内顺序。</param>
        public void AddCapability(Capability capability, int tickGroup, int tickOrder)
        {
            if (!m_GroupMap.TryGetValue(tickGroup, out var group))
            {
                group = new Group(tickGroup);
                m_GroupMap.Add(tickGroup, group);
                m_Groups.Add(group);
                m_Groups.Sort((a, b) => a.id.CompareTo(b.id));
            }
            m_Positions.Add(capability, (group, group.entries.Count));
            group.entries.Add(new Entry(capability, tickOrder, m_Sequence++));
            group.dirty = true;
        }

        /// <summary>
        ///   <para>解除登记并立即清除队列对能力的引用。</para>
        /// </summary>
        /// <param name="capability">待移除能力。</param>
        public bool RemoveCapability(Capability capability)
        {
            if (!m_Positions.TryGetValue(capability, out var position)) return false;
            position.group.entries[position.index] = default;
            position.group.dirty = true;
            return m_Positions.Remove(capability);
        }

        /// <summary>
        ///   <para>更新全部已登记阶段。</para>
        /// </summary>
        /// <param name="deltaTime">帧间隔。</param>
        /// <param name="activate">激活条件。</param>
        /// <param name="deactivate">停用条件。</param>
        /// <param name="failed">能力失败后的移除请求。</param>
        public void Update(float deltaTime, Func<Capability, bool> activate, Func<Capability, bool> deactivate, Action<Capability> failed)
        {
            for (int i = 0; i < m_Groups.Count; i++)
                UpdateGroup(m_Groups[i], deltaTime, activate, deactivate, failed);
        }

        /// <summary>
        ///   <para>仅检查和更新指定阶段。</para>
        /// </summary>
        /// <param name="deltaTime">帧间隔。</param>
        /// <param name="activate">激活条件。</param>
        /// <param name="deactivate">停用条件。</param>
        /// <param name="tickGroup">目标阶段。</param>
        /// <param name="failed">能力失败后的移除请求。</param>
        public void Update(float deltaTime, Func<Capability, bool> activate, Func<Capability, bool> deactivate, int tickGroup, Action<Capability> failed)
        {
            if (m_GroupMap.TryGetValue(tickGroup, out var group))
                UpdateGroup(group, deltaTime, activate, deactivate, failed);
        }

        /// <summary>
        ///   <para>清空全部调度引用。</para>
        /// </summary>
        public void Clear()
        {
            foreach (var group in m_Groups) group.entries.Clear();
            m_Positions.Clear();
        }

        /// <summary>
        ///   <para>执行阶段；每个能力依次完成条件判断、生命周期和 Tick。</para>
        /// </summary>
        /// <param name="group">调度组。</param>
        /// <param name="deltaTime">帧间隔。</param>
        /// <param name="activate">激活条件。</param>
        /// <param name="deactivate">停用条件。</param>
        /// <param name="failed">失败移除请求。</param>
        private void UpdateGroup(Group group, float deltaTime, Func<Capability, bool> activate, Func<Capability, bool> deactivate, Action<Capability> failed)
        {
            var entries = group.entries;
            if (group.dirty)
            {
                entries.RemoveAll(entry => entry.capability == null);
                entries.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.sequence.CompareTo(b.sequence));
                for (int i = 0; i < entries.Count; i++) m_Positions[entries[i].capability] = (group, i);
                group.dirty = false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var capability = entries[i].capability;
                if (capability.IsRemovalPending) continue;
                try
                {
                    if (capability.IsActive)
                    {
                        if (deactivate(capability)) capability.Deactivate();
                    }
                    else if (activate(capability))
                    {
                        capability.IsActive = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        capability.OwnerWorld?.DebugTrace.Record(
                            capability.OwnerActor,
                            capability,
                            CapabilityDebugEventKind.Activated);
#endif
                        capability.OnActivated();
                    }

                    // 条件或激活回调可能请求移除自身，不能再执行 Tick。
                    if (capability.IsActive && !capability.IsRemovalPending)
                        capability.TickActive(deltaTime);
                }
                catch { failed(capability); throw; }
            }
        }
    }
}