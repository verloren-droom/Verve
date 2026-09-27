// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>标签标识符；用于能力阻塞机制。</para>
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public readonly struct TagId : IEquatable<TagId>, IComparable<TagId>
    {
        /// <summary>
        ///   <para>空标签标识。</para>
        /// </summary>
        public static readonly TagId none = new TagId(0);
        /// <summary>
        ///   <para>标签标识值；零表示空标签。</para>
        /// </summary>
        private readonly int m_Value;

        /// <summary>
        ///   <para>从整数值创建标签标识符。</para>
        /// </summary>
        /// <param name="value">标识值。</param>
        public TagId(int value) => m_Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(TagId left, TagId right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(TagId left, TagId right) => !left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(TagId tagId) => tagId.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(TagId other) => m_Value == other.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is TagId other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(TagId other) => m_Value.CompareTo(other.m_Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => m_Value == 0 ? "Tag(None)" : $"Tag({m_Value})";
    }
}