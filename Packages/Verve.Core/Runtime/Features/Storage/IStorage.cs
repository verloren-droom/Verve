namespace Verve.Storage
{
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    
    /// <summary>
    /// 存储接口
    /// </summary>
    public interface IStorage : System.IDisposable
    {
        /// <summary>
        /// 数据序列化委托
        /// </summary>
        delegate byte[] SerializerDelegate(object data);
        /// <summary>
        /// 数据反序列化委托
        /// </summary>
        delegate TData DeserializerDelegate<out TData>(byte[] data);

        
        /// <summary>
        /// 默认文件扩展名
        /// </summary>
        public string DefaultFileExtension { get; set; }

        /// <summary>
        /// 尝试读取数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">数据键</param>
        /// <param name="outValue">输出数据</param>
        /// <param name="defaultValue">默认值</param>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <returns></returns>
        [System.Obsolete("Please use async version for large data")]
        bool TryReadData<TData>(
            string filePath,
            string key,
            out TData outValue,
            Encoding encoding,
            DeserializerDelegate<TData> deserializer,
            TData defaultValue = default);
        
        /// <summary>
        /// 写入数据
        /// </summary>
        [System.Obsolete("Please use async version for large data")]
        void WriteData<TData>(
            string filePath,
            string key,
            TData value,
            Encoding encoding,
            SerializerDelegate serializer,
            DeserializerDelegate<TData> deserializer);
        
        /// <summary>
        /// 删除指定数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">数据键</param>
        void DeleteData<TData>(string filePath, string key, Encoding encoding, DeserializerDelegate<TData> deserializer);

        /// <summary>
        /// 删除所有数据
        /// </summary>
        void DeleteAllData(string filePath);
        
        /// <summary>
        /// 检查数据是否存在
        /// </summary>
        bool HasData(string filePath, string key, DeserializerDelegate<object> deserializer);

        /// <summary>
        /// 异步读取数据
        /// </summary>
        Task<TData> ReadDataAsync<TData>(
            string filePath,
            string key,
            Encoding encoding,
            DeserializerDelegate<TData> deserializer,
            TData defaultValue = default,
            CancellationToken ct = default);
        
        /// <summary>
        /// 异步写入数据
        /// </summary>
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