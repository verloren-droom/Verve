namespace Verve
{
    using System;

    /// <summary>
    ///   <para>事件键；保留字符串或整数原值，两种类型互不冲突。</para>
    /// </summary>
    public readonly struct EventKey : IEquatable<EventKey>
    {
        /// <summary>
        ///   <para>名称。</para>
        /// </summary>
        private readonly string m_Name;
        /// <summary>
        ///   <para>标识。</para>
        /// </summary>
        private readonly int m_Id;

        /// <summary>
        ///   <para>创建事件键。</para>
        /// </summary>
        /// <param name="name">名称。</param>
        public EventKey(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Event name cannot be empty.", nameof(name));
            m_Name = name;
            m_Id = 0;
        }

        /// <summary>
        ///   <para>创建事件键。</para>
        /// </summary>
        /// <param name="id">标识。</param>
        public EventKey(int id) { m_Id = id; m_Name = null; }

        public bool Equals(EventKey other) => m_Id == other.m_Id && string.Equals(m_Name, other.m_Name, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EventKey other && Equals(other);
        public override int GetHashCode() => m_Name == null ? m_Id : StringComparer.Ordinal.GetHashCode(m_Name);
        public override string ToString() => m_Name ?? $"#{m_Id}";
    }
}