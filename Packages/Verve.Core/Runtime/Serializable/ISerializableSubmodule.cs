namespace Verve.Serializable
{
    using System.IO;

    
    /// <summary>
    /// 序列化子模块接口
    /// </summary>
    public interface ISerializableSubmodule : IGameFeatureSubmodule
    {
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Deserialize<T>(byte[] value);
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        byte[] Serialize(object obj);
        
        void Serialize(Stream stream, object obj);
        
        T DeserializeFromStream<T>(Stream stream);
    }
}