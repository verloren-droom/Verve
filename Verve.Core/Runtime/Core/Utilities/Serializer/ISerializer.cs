namespace Verve
{
    using System.IO;
    
    
    /// <summary>
    ///   <para>序列化接口</para>
    /// </summary>
    public interface ISerializer
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
        T Deserialize<T>(Stream stream);
    }
}