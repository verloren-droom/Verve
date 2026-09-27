// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>组件类型标识符；仅用于进程内组件索引。</para>
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public readonly struct ComponentTypeId : IEquatable<ComponentTypeId>, IComparable<ComponentTypeId>
    {
        /// <summary>
        ///   <para>空组件类型标识符。</para>
        /// </summary>
        public static readonly ComponentTypeId none = new ComponentTypeId(0);
        /// <summary>
        ///   <para>组件类型标识值；零表示未注册。</para>
        /// </summary>
        private readonly int m_Value;

        /// <summary>
        ///   <para>从整数值创建组件类型标识符。</para>
        /// </summary>
        /// <param name="value">标识值。</param>
        public ComponentTypeId(int value) => m_Value = value;

        /// <summary>
        ///   <para>获取组件类型对应的注册标识。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ComponentTypeId Create<T>() where T : struct, IComponent
            => ComponentTypeRegistry<T>.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(ComponentTypeId left, ComponentTypeId right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(ComponentTypeId left, ComponentTypeId right) => !left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator int(ComponentTypeId componentTypeId) => componentTypeId.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(ComponentTypeId other) => m_Value == other.m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is ComponentTypeId other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int CompareTo(ComponentTypeId other) => m_Value.CompareTo(other.m_Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => m_Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => m_Value == 0 ? "ComponentType(None)" : $"ComponentType({m_Value})";
    }
}
