#if UNITY_EDITOR || DEVELOPMENT_BUILD

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>能力调试事件类型；记录能力状态变化和异常。</para>
    /// </summary>
    internal enum CapabilityDebugEventKind : byte
    {
        /// <summary>
        ///   <para>能力已加入行动者。</para>
        /// </summary>
        Added,
        /// <summary>
        ///   <para>能力已激活。</para>
        /// </summary>
        Activated,
        /// <summary>
        ///   <para>能力激活条件被阻塞。</para>
        /// </summary>
        Blocked,
        /// <summary>
        ///   <para>能力已停用。</para>
        /// </summary>
        Deactivated,
        /// <summary>
        ///   <para>能力已移除。</para>
        /// </summary>
        Removed,
        /// <summary>
        ///   <para>能力更新发生异常。</para>
        /// </summary>
        Failed,
    }

    /// <summary>
    ///   <para>能力调试事件；只保存类型和句柄，不持有能力实例。</para>
    /// </summary>
    internal readonly struct CapabilityDebugEvent
    {
        /// <summary>
        ///   <para>递增事件序号。</para>
        /// </summary>
        public readonly long sequence;
        /// <summary>
        ///   <para>事件对应的行动者。</para>
        /// </summary>
        public readonly Actor actor;
        /// <summary>
        ///   <para>能力类型。</para>
        /// </summary>
        public readonly Type capabilityType;
        /// <summary>
        ///   <para>事件类型。</para>
        /// </summary>
        public readonly CapabilityDebugEventKind kind;
        /// <summary>
        ///   <para>记录事件时的帧号。</para>
        /// </summary>
        public readonly int frame;
        /// <summary>
        ///   <para>记录事件时的时间。</para>
        /// </summary>
        public readonly float time;
        /// <summary>
        ///   <para>异常类型和消息；没有异常时为空。</para>
        /// </summary>
        public readonly string error;

        /// <summary>
        ///   <para>创建能力调试事件。</para>
        /// </summary>
        /// <param name="sequence">事件序号。</param>
        /// <param name="actor">行动者。</param>
        /// <param name="capabilityType">能力类型。</param>
        /// <param name="kind">事件类型。</param>
        /// <param name="frame">帧号。</param>
        /// <param name="time">时间。</param>
        /// <param name="error">异常信息。</param>
        public CapabilityDebugEvent(
            long sequence,
            Actor actor,
            Type capabilityType,
            CapabilityDebugEventKind kind,
            int frame,
            float time,
            string error)
        {
            this.sequence = sequence;
            this.actor = actor;
            this.capabilityType = capabilityType;
            this.kind = kind;
            this.frame = frame;
            this.time = time;
            this.error = error;
        }
    }

    /// <summary>
    ///   <para>世界能力调试记录；使用固定环形缓冲区避免调试自身制造持续分配。</para>
    /// </summary>
    internal sealed class CapabilityDebugTrace
    {
        /// <summary>
        ///   <para>默认事件容量。</para>
        /// </summary>
        private const int Capacity = 256;

        /// <summary>
        ///   <para>事件环形缓冲区。</para>
        /// </summary>
        private readonly CapabilityDebugEvent[] m_Events = new CapabilityDebugEvent[Capacity];
        /// <summary>
        ///   <para>当前持有记录权的调试工具。</para>
        /// </summary>
        private readonly HashSet<object> m_Owners = new(ReferenceEqualityComparer<object>.Instance);
        /// <summary>
        ///   <para>下一个写入位置。</para>
        /// </summary>
        private int m_NextIndex;
        /// <summary>
        ///   <para>当前事件数量。</para>
        /// </summary>
        private int m_Count;
        /// <summary>
        ///   <para>下一个事件序号。</para>
        /// </summary>
        private long m_NextSequence;

        /// <summary>
        ///   <para>是否正在记录事件。</para>
        /// </summary>
        internal bool IsEnabled => m_Owners.Count != 0;
        /// <summary>
        ///   <para>当前缓冲区中的事件数量。</para>
        /// </summary>
        internal int Count => m_Count;

        /// <summary>
        ///   <para>事件写入通知；供日志接收完整事件流，不受环形缓冲区覆盖影响。</para>
        /// </summary>
        internal event Action<CapabilityDebugEvent> Recorded;

        /// <summary>
        ///   <para>设置一个调试工具的记录权。</para>
        /// </summary>
        /// <param name="owner">记录权所有者。</param>
        /// <param name="enabled">是否持有记录权。</param>
        internal void SetOwnerEnabled(object owner, bool enabled)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (enabled) m_Owners.Add(owner);
            else m_Owners.Remove(owner);
        }

        /// <summary>
        ///   <para>清除所有调试工具的记录权。</para>
        /// </summary>
        internal void DisableAll()
            => m_Owners.Clear();

        /// <summary>
        ///   <para>记录一个能力事件。</para>
        /// </summary>
        /// <param name="actor">行动者。</param>
        /// <param name="capability">能力实例；只读取类型，不保存实例。</param>
        /// <param name="kind">事件类型。</param>
        /// <param name="exception">关联异常。</param>
        internal void Record(Actor actor, Capability capability, CapabilityDebugEventKind kind, Exception exception = null)
        {
            if (!IsEnabled || capability == null)
                return;

            var error = exception == null ? null : $"{exception.GetType().Name}: {exception.Message}";
            RecordCore(actor, capability, kind, error);
        }

        /// <summary>
        ///   <para>记录带说明文本的能力事件。</para>
        /// </summary>
        /// <param name="actor">行动者。</param>
        /// <param name="capability">能力实例；只读取类型，不保存实例。</param>
        /// <param name="kind">事件类型。</param>
        /// <param name="error">说明文本。</param>
        internal void Record(Actor actor, Capability capability, CapabilityDebugEventKind kind, string error)
        {
            if (!IsEnabled || capability == null)
                return;
            RecordCore(actor, capability, kind, error);
        }

        /// <summary>
        ///   <para>写入调试事件。</para>
        /// </summary>
        /// <param name="actor">行动者。</param>
        /// <param name="capability">能力实例。</param>
        /// <param name="kind">事件类型。</param>
        /// <param name="error">说明文本。</param>
        private void RecordCore(Actor actor, Capability capability, CapabilityDebugEventKind kind, string error)
        {
            var item = new CapabilityDebugEvent(
                ++m_NextSequence,
                actor,
                capability.GetType(),
                kind,
                GetFrame(),
                GetTime(),
                error);
            m_Events[m_NextIndex] = item;
            m_NextIndex = (m_NextIndex + 1) % m_Events.Length;
            if (m_Count < m_Events.Length)
                m_Count++;
            Recorded?.Invoke(item);
        }

        /// <summary>
        ///   <para>复制最近事件；结果按时间从旧到新排列。</para>
        /// </summary>
        /// <param name="output">接收事件的列表。</param>
        internal void CopyTo(List<CapabilityDebugEvent> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            var start = (m_NextIndex - m_Count + m_Events.Length) % m_Events.Length;
            for (var i = 0; i < m_Count; i++)
                output.Add(m_Events[(start + i) % m_Events.Length]);
        }

        /// <summary>
        ///   <para>清空事件记录。</para>
        /// </summary>
        internal void Clear()
        {
            Array.Clear(m_Events, 0, m_Events.Length);
            m_NextIndex = 0;
            m_Count = 0;
            m_NextSequence = 0;
        }

        private static int GetFrame()
        {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.Time.frameCount;
#else
            return 0;
#endif
        }

        private static float GetTime()
        {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.Time.realtimeSinceStartup;
#else
            return (float)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
#endif
        }
    }
}

#endif
