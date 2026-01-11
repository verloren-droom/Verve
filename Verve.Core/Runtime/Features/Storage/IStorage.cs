namespace Verve.Storage
{
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    
    /// <summary>
    ///   <para>存储接口</para>
    /// </summary>
    public interface IStorage : System.IDisposable
    {
        /// <summary>
        ///   <para>数据序列化委托</para>
        /// </summary>
        delegate byte[] SerializerDelegate(object data);
        /// <summary>
        ///   <para>数据反序列化委托</para>
        /// </summary>
        delegate TData DeserializerDelegate<out TData>(byte[] data);

        /// <summary>
        ///   <para>尝试读取数据</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">数据键</param>
        /// <param name="outValue">输出数据</param>
        /// <param name="defaultValue">默认值</param>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <returns>
        ///   <para>是否成功</para>
        /// </returns>
        [System.Obsolete("Please use async version for large data")]
        bool TryReadData<TData>(
            string filePath,
            string key,
            out TData outValue,
            Encoding encoding,
            DeserializerDelegate<TData> deserializer,
            TData defaultValue = default);
        
        /// <summary>
        ///   <para>写入数据</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">数据键</param>
        /// <param name="value">数据值</param>
        /// <param name="encoding">编码</param>
        /// <typeparam name="TData">数据类型</typeparam>
        [System.Obsolete("Please use async version for large data")]
        void WriteData<TData>(
            string filePath,
            string key,
            TData value,
            Encoding encoding,
            SerializerDelegate serializer,
            DeserializerDelegate<TData> deserializer);
        
        /// <summary>
        ///   <para>删除指定数据</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">数据键</param>
        /// <param name="encoding">编码</param>
        void DeleteData<TData>(string filePath, string key, Encoding encoding, DeserializerDelegate<TData> deserializer);

        /// <summary>
        ///   <para>删除所有数据</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        void DeleteAllData(string filePath);
        
        /// <summary>
        ///   <para>检查数据是否存在</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">数据键</param>
        bool HasData(string filePath, string key, DeserializerDelegate<object> deserializer);

        /// <summary>
        ///   <para>异步读取数据</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">数据键</param>
        /// <param name="encoding">编码</param>
        Task<TData> ReadDataAsync<TData>(
            string filePath,
            string key,
            Encoding encoding,
            DeserializerDelegate<TData> deserializer,
            TData defaultValue = default,
            CancellationToken ct = default);
        
        /// <summary>
        ///   <para>异步写入数据</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">数据键</param>
        /// <param name="value">数据值</param>
        /// <param name="encoding">编码</param>
        Task WriteDataAsync<TData>(
            string filePath,
            string key,
            TData value,
            Encoding encoding,
            SerializerDelegate serializer,
            DeserializerDelegate<TData> deserializer,
            CancellationToken ct = default);
    }
}