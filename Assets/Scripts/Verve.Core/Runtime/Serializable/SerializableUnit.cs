namespace Verve.Serializable
{
    using Unit;
    using System;
    using System.IO;
    using System.Collections.Concurrent;
    using System.Runtime.Serialization.Formatters.Binary;
    
    
    /// <summary>
    /// 序列化单元
    /// </summary>
    [CustomUnit("Serializable"), System.Serializable]
    public sealed partial class SerializableUnit : UnitBase
    {
        private ConcurrentDictionary<Type, ISerializableConverter> m_SerializableConverters =
            new ConcurrentDictionary<Type, ISerializableConverter>();

        public override void Startup(UnitRules parent, params object[] args)
        {
            base.Startup(parent, args);
            m_SerializableConverters.TryAdd(typeof(JsonSerializableConverter), Activator.CreateInstance<JsonSerializableConverter>());
            m_SerializableConverters.TryAdd(typeof(CustomSerializableConverter),
                Activator.CreateInstance<CustomSerializableConverter>());
        }

        public TValue Deserialize<TSerializable, TValue>(string value) where TSerializable : ISerializableConverter
        {
            if (m_SerializableConverters.TryGetValue(typeof(TSerializable), out var converter))
            {
                return converter.Deserialize<TValue>(value);
            }
            throw new ArgumentException();
        }

        public string Serialize<TSerializable>(object obj) where TSerializable : ISerializableConverter
        {
            if (m_SerializableConverters.TryGetValue(typeof(TSerializable), out var converter))
            {
                return converter.Serialize(obj);
            }
            throw new ArgumentException();
        }
        
        public byte[] Serialize<T>(T data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, data);
                return ms.ToArray();
            }
        }
        
        public T Deserialize<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}