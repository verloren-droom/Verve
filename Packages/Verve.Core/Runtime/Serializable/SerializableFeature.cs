namespace Verve.Serializable
{
    using System.IO;
    using System.Text;
    
    
    /// <summary>
    /// 序列化功能
    /// </summary>
    [System.Serializable]
    public class SerializableFeature : ModularGameFeature
    {
        protected override void OnLoad()
        {
            base.OnLoad();
            
            // RegisterSubmodule(new CsvSerializableService());
            RegisterSubmodule(new ProtoBufSerializableSubmodule());
            RegisterSubmodule(new JsonSerializableSubmodule());
        }

        public TValue Deserialize<TSerializable, TValue>(string value) 
            where TSerializable : class, ISerializableSubmodule
        {
            return string.IsNullOrEmpty(value) ? default : Deserialize<TSerializable, TValue>(Encoding.UTF8.GetBytes(value));
        }

        public TValue Deserialize<TSerializable, TValue>(byte[] value)
            where TSerializable : class, ISerializableSubmodule
        {
            return GetSubmodule<TSerializable>().Deserialize<TValue>(value);
        }

        public TValue DeserializeFromStream<TSerializable, TValue>(Stream stream) where TSerializable : class, ISerializableSubmodule
        {
            return GetSubmodule<TSerializable>().DeserializeFromStream<TValue>(stream);
        }

        public string Serialize<TSerializable>(object obj) where TSerializable : class, ISerializableSubmodule
        {
            return Encoding.UTF8.GetString(SerializeToBytes<TSerializable>(obj));
        }
        
        public byte[] SerializeToBytes<TSerializable>(object obj) where TSerializable : class, ISerializableSubmodule
        {
            return GetSubmodule<TSerializable>()?.Serialize(obj);
        }

        public void Serialize<TSerializable>(Stream stream, object obj) where TSerializable : class, ISerializableSubmodule
        {
            GetSubmodule<TSerializable>()?.Serialize(stream, obj);
        }
    }
}