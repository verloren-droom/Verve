namespace Verve.Serializable
{
    using System.IO;

    
    /// <summary>
    /// 序列化子模块接口
    /// </summary>
    public interface ISerializableSubmodule : IGameFeatureSubmodule
    {
        /// <summary>
        /// 序列化
        /// </summary>
        void Serialize(Stream stream, object obj);
        
        /// <summary>
        /// 反序列化
        /// </summary>
        T Deserialize<T>(Stream stream);
    }
}