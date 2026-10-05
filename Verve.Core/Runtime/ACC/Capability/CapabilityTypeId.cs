// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>能力类型标识符；仅用于进程内能力索引。</para>
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public readonly struct CapabilityTypeId : IEquatable<CapabilityTypeId>, IComparable<CapabilityTypeId>
    {
        /// <summary>
        ///   <para>空能力类型标识。</para>
        /// </summary>
        public static readonly CapabilityTypeId none = new CapabilityTypeId(0);
        /// <summary>
        ///   <para>能力类型标识值；零表示未注册。</para>
        /// </summary>
        private readonly int m_Value;

        /// <summary>
        ///   <para>从整数值创建能力类型标识符。</para>
        /// </summary>
        /// <param name="value">标识值。</param>
        public CapabilityTypeId(int value) => m_Value = value;

        /// <summary>
        ///   <para>获取能力类型对应的注册标识。</para>
        /// </summary>
        /// <typeparam name="T">能力类型。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CapabilityTypeId Create<T>() where T : Capability
            => CapabilityTypeRegistry<T>.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CapabilityTypeId left, CapabilityTypeId right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CapabilityTypeId left, CapabilityTypeId right) => !left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(CapabilityTypeId capabilityTypeId) => capabilityTypeId.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(CapabilityTypeId other) => m_Value == other.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is CapabilityTypeId other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(CapabilityTypeId other) => m_Value.CompareTo(other.m_Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => m_Value == 0 ? "CapabilityType(None)" : $"CapabilityType({m_Value})";
    }
}