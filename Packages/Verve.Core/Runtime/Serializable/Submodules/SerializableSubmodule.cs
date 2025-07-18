namespace Verve.Serializable
{
    using System.IO;
    
    
    /// <summary>
    /// 序列化子模块基类
    /// </summary>
    public abstract class SerializableSubmodule : ISerializableSubmodule
    {
        public abstract string ModuleName { get; }
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies) { }
        public virtual void OnModuleUnloaded() { }

        public abstract void Serialize(Stream stream, object obj);

        public abstract T Deserialize<T>(Stream stream);
    }
}