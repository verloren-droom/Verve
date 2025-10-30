namespace Verve.Serializable
{
    using System.IO;

    
    /// <summary>
    ///   <para>序列化接口</para>
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        ///   <para>序列化</para>
        /// </summary>
        /// <param name="stream">数据流</param>
        /// <param name="obj">对象</param>
        void Serialize(Stream stream, object obj);
        
        /// <summary>
        ///   <para>反序列化</para>
        /// </summary>
        /// <param name="stream">数据流</param>
        /// <returns>
        ///   <para>对象</para>
        /// </returns>
        T Deserialize<T>(Stream stream);
    }
}