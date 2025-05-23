namespace Verve.Serializable
{
    
    using Unit;
    using System.IO;
    using System.Text;
    
    
    /// <summary>
    /// 序列化单元
    /// </summary>
    [CustomUnit("Serializable"), System.Serializable]
    public partial class SerializableUnit : UnitBase<ISerializableConverter>
    {
        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            AddService(new JsonSerializableConverter());
            AddService(new ProtoBufSerializableConverter());
        }

        public TValue Deserialize<TSerializable, TValue>(string value) 
            where TSerializable : class, ISerializableConverter
        {
            return string.IsNullOrEmpty(value) ? default : Deserialize<TSerializable, TValue>(Encoding.UTF8.GetBytes(value));
        }

        public TValue Deserialize<TSerializable, TValue>(byte[] value)
            where TSerializable : class, ISerializableConverter
        {
            return GetService<TSerializable>().Deserialize<TValue>(value);
        }

        public TValue DeserializeFromStream<TSerializable, TValue>(Stream stream) where TSerializable : class, ISerializableConverter
        {
            return GetService<TSerializable>().DeserializeFromStream<TValue>(stream);
        }

        public string Serialize<TSerializable>(object obj) where TSerializable : class, ISerializableConverter
        {
            return Encoding.UTF8.GetString(SerializeToBytes<TSerializable>(obj));
        }
        
        public byte[] SerializeToBytes<TSerializable>(object obj) where TSerializable : class, ISerializableConverter
        {
            return GetService<TSerializable>()?.Serialize(obj);
        }

        public void Serialize<TSerializable>(Stream stream, object obj) where TSerializable : class, ISerializableConverter
        {
            GetService<TSerializable>()?.Serialize(stream, obj);
        }
    }
}