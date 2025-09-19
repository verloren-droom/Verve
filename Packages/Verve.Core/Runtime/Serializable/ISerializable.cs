namespace Verve.Serializable
{
    using System.IO;

    
    /// <summary>
    /// 序列化接口
    /// </summary>
    public interface ISerializable
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