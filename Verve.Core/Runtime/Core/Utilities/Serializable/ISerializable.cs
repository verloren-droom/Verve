namespace Verve
{
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
        void Serialize(System.IO.Stream stream, object obj);
        
        /// <summary>
        ///   <para>反序列化</para>
        /// </summary>
        /// <param name="stream">数据流</param>
        T Deserialize<T>(System.IO.Stream stream);
    }
}