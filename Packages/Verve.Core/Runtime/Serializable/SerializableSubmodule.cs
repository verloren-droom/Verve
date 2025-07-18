namespace Verve.Serializable
{
    using System.IO;
    
    
    /// <summary>
    /// 序列化子模块基类
    /// </summary>
    public abstract class SerializableSubmodule : ISerializableSubmodule
    {
        public abstract T Deserialize<T>(byte[] value);
        public abstract byte[] Serialize(object obj);
        
        public virtual void Serialize(Stream stream, object obj)
        {
            throw new System.NotImplementedException();
        }

        public virtual T DeserializeFromStream<T>(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public abstract string ModuleName { get; }
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies) { }
        public virtual void OnModuleUnloaded() { }
    }
}