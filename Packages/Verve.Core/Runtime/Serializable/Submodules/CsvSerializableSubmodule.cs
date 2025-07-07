namespace Verve.Serializable
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Buffers;
    using System.Reflection;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    
    
    // public sealed class CsvSerializableSubmodule : SerializableServiceBase
    // {
    //     private const int InitialBufferSize = 4096;
    //     private static readonly Encoding Utf8 = Encoding.UTF8;
    //
    //     public override unsafe T Deserialize<T>(byte[] bytes)
    //     {
    //         T result = default;
    //         var span = bytes.AsSpan();
    //         
    //         fixed (byte* ptr = span)
    //         {
    //             var parser = new CsvParser<T>(ptr, span.Length);
    //             parser.Parse(ref result);
    //         }
    //         
    //         return result;
    //     }
    //
    //     public override byte[] Serialize(object obj)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     private unsafe struct CsvParser<T> where T : unmanaged
    //     {
    //         private readonly byte* _source;
    //         private readonly int _length;
    //         private int _position;
    //         private int _fieldStart;
    //         private bool _inQuotes;
    //
    //         public CsvParser(byte* source, int length)
    //         {
    //             _source = source;
    //             _length = length;
    //             _position = 0;
    //             _fieldStart = 0;
    //             _inQuotes = false;
    //         }
    //
    //         public void Parse(ref T obj)
    //         {
    //             int fieldIndex = 0;
    //             var fields = typeof(T).GetFields()
    //                 .OrderBy(f => f.GetCustomAttribute<CsvColumnAttribute>()?.Index ?? int.MaxValue)
    //                 .ToArray();
    //
    //             while (_position < _length)
    //             {
    //                 byte c = _source[_position];
    //                 
    //                 if (c == '"')
    //                 {
    //                     _inQuotes = !_inQuotes;
    //                 }
    //                 else if (c == ',' && !_inQuotes && fieldIndex < fields.Length)
    //                 {
    //                     ProcessField(ref obj, fields[fieldIndex++]);
    //                     _fieldStart = _position + 1;
    //                 }
    //                 else if ((c == '\n' || c == '\r') && fieldIndex < fields.Length)
    //                 {
    //                     ProcessField(ref obj, fields[fieldIndex++]);
    //                     break;
    //                 }
    //
    //                 _position++;
    //             }
    //         }
    //
    //         [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //         private void ProcessField(ref T obj, FieldInfo field)
    //         {
    //             var valueSpan = new ReadOnlySpan<byte>(_source + _fieldStart, _position - _fieldStart);
    //             var str = Utf8.GetString(valueSpan).Trim('"', ' ');
    //             
    //             fixed (byte* pObj = &Unsafe.As<T, byte>(ref obj))
    //             {
    //                 byte* fieldPtr = pObj + Marshal.OffsetOf(field.DeclaringType, field.Name).ToInt32();
    //                 ParseValue(fieldPtr, field.FieldType, str);
    //             }
    //         }
    //
    //         private static void ParseValue(byte* dest, Type type, string value)
    //         {
    //             // 类型安全的解析实现...
    //         }
    //     }
    // }


    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CsvIgnoreAttribute : Attribute { }
    
    
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CsvColumnAttribute : Attribute
    {
        public int Index { get; }
        public CsvColumnAttribute(int index) => Index = index;
    }
}