namespace Verve.Serializable
{
    
    using Unit;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    
    
    /// <summary>
    /// 序列化单元
    /// </summary>
    [CustomUnit("Serializable"), System.Serializable]
    public partial class SerializableUnit : UnitBase<ISerializableConverter>
    {
        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
            Register(new JsonSerializableConverter());
        }

        public TValue Deserialize<TSerializable, TValue>(string value) where TSerializable : class, ISerializableConverter
        {
            return Resolve<TSerializable>().Deserialize<TValue>(value);
        }

        public string Serialize<TSerializable>(object obj) where TSerializable : class, ISerializableConverter
        {
            return Resolve<TSerializable>()?.Serialize(obj);
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