namespace Verve
{
    using System.IO;
    using System.Text;
    
    /// <summary>
    ///   <para>序列化接口。</para>
    /// </summary>
    [GameTool("序列化", typeof(JsonSerializer))]
    public interface ISerializer : IGameTool
    {
        /// <summary>
        ///   <para>序列化。</para>
        /// </summary>
        /// <param name="stream">数据流。</param>
        /// <param name="obj">对象。</param>
        /// <param name="encoding">编码。</param>
        void Serialize(Stream stream, object obj, Encoding encoding = null);
        /// <summary>
        ///   <para>反序列化。</para>
        /// </summary>
        /// <param name="stream">数据流。</param>
        /// <param name="encoding">编码。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        T Deserialize<T>(Stream stream, Encoding encoding = null);
    }
}