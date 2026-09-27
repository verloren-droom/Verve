// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Buffers.Binary;
    using System.Runtime.InteropServices;

    /// <summary>
    ///   <para>自动组件编码；注册时解析数值字段，收发时仅按缓存区域复制，不发送内存填充。</para>
    /// </summary>
    /// <typeparam name="T">只包含定宽数值、枚举或其嵌套结构的组件。</typeparam>
    internal sealed class ReplicationCodec<T> : IReplicationCodec<T> where T : unmanaged, IComponent
    {
        /// <summary>
        ///   <para>数值字段；保存本机偏移和协议宽度。</para>
        /// </summary>
        private readonly struct Field
        {
            internal readonly int Offset;
            internal readonly int Size;
            internal Field(int offset, int size) { Offset = offset; Size = size; }
        }

        /// <summary>
        ///   <para>按字段名排序的数值区域；顺序不依赖反射返回顺序或 ABI 对齐。</para>
        /// </summary>
        private readonly Field[] m_Fields;

        /// <inheritdoc />
        public int Size { get; }
        /// <summary>
        ///   <para>字段路径和数值类型的协议指纹。</para>
        /// </summary>
        internal ulong Format { get; private set; } = Game.HashUtility.ComputeFnv1a64(ReadOnlySpan<byte>.Empty);

        /// <summary>
        ///   <para>预编排字段；不使用动态代码生成，支持 AOT。</para>
        /// </summary>
        internal ReplicationCodec()
        {
            var fields = new List<Field>();
            Collect(typeof(T), 0, fields);
            m_Fields = fields.ToArray();
            foreach (var field in m_Fields) Size = checked(Size + field.Size);
            if (Size == 0) throw new NotSupportedException($"{typeof(T)} has no numeric fields; provide a codec for marker components.");
            T value = default;
            int memorySize = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1)).Length;
            foreach (var field in m_Fields)
                if (field.Offset < 0 || field.Size > memorySize - field.Offset)
                    throw new NotSupportedException($"{typeof(T)} has an unsupported managed layout; provide a codec.");
        }

        /// <inheritdoc />
        public void Encode(Span<byte> destination, T value)
        {
            if (destination.Length != Size) throw new ArgumentException("Incorrect component encoding length.", nameof(destination));
            var memory = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
            foreach (var field in m_Fields)
            {
                CopyNumber(memory.Slice(field.Offset, field.Size), destination.Slice(0, field.Size));
                destination = destination.Slice(field.Size);
            }
        }

        /// <inheritdoc />
        public T Decode(ReadOnlySpan<byte> source)
        {
            if (source.Length != Size) throw new ArgumentException("Incorrect component encoding length.", nameof(source));
            T value = default;
            var memory = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
            foreach (var field in m_Fields)
            {
                CopyNumber(source.Slice(0, field.Size), memory.Slice(field.Offset, field.Size));
                source = source.Slice(field.Size);
            }
            return value;
        }

        /// <summary>
        ///   <para>转换本机和小端序；数值以固定位宽传输。</para>
        /// </summary>
        /// <param name="source">源字段。</param>
        /// <param name="destination">目标字段。</param>
        private static void CopyNumber(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (BitConverter.IsLittleEndian) source.CopyTo(destination);
            else for (int i = 0; i < source.Length; i++) destination[i] = source[source.Length - 1 - i];
        }

        /// <summary>
        ///   <para>追加 UTF-16 小端文本和单字节分隔标记；保持协议格式确定。</para>
        /// </summary>
        /// <param name="text">字段名或数值类型。</param>
        private void AppendFormat(string text)
        {
            Span<byte> bytes = stackalloc byte[sizeof(char)];
            foreach (char value in text)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
                Format = Game.HashUtility.ComputeFnv1a64(bytes, Format);
            }
            bytes[0] = 255;
            Format = Game.HashUtility.ComputeFnv1a64(bytes.Slice(0, 1), Format);
        }

        /// <summary>
        ///   <para>解析可直接访问的数值布局；拒绝指针、联合体和有封送转换的字段。</para>
        /// </summary>
        /// <param name="type">当前字段类型。</param>
        /// <param name="offset">本机累计偏移。</param>
        /// <param name="fields">编排结果。</param>
        private void Collect(Type type, int offset, List<Field> fields)
        {
            if (type.IsEnum) type = Enum.GetUnderlyingType(type);
            int width = Type.GetTypeCode(type) switch
            {
                TypeCode.Byte or TypeCode.SByte => 1,
                TypeCode.Int16 or TypeCode.UInt16 => 2,
                TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Single => 4,
                TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double => 8,
                _ => 0,
            };
            if (width != 0)
            {
                AppendFormat(type.FullName);
                fields.Add(new Field(offset, width));
                return;
            }
            if (!type.IsValueType || !type.IsLayoutSequential || type.IsPrimitive || type.IsPointer ||
                type == typeof(decimal) || type == typeof(IntPtr) || type == typeof(UIntPtr))
                throw new NotSupportedException($"Automatic replication cannot encode {type}; provide IReplicationCodec<{typeof(T).Name}>.");
            var members = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Array.Sort(members, (a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));
            foreach (var member in members)
            {
                if (member.IsDefined(typeof(MarshalAsAttribute), false) ||
                    member.IsDefined(typeof(System.Runtime.CompilerServices.FixedBufferAttribute), false))
                    throw new NotSupportedException($"Field {type}.{member.Name} requires an explicit replication codec.");
                AppendFormat(member.Name);
                Collect(member.FieldType, checked(offset + Marshal.OffsetOf(type, member.Name).ToInt32()), fields);
            }
            AppendFormat(";");
        }
    }
}
