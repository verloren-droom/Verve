namespace Verve.Storage
{
    using System.Text;


    /// <summary>
    ///   <para>存储扩展类</para>
    ///   <para>用于简化存储操作</para>
    /// </summary>
    public static class StorageExtensions
    {
        /// <summary>
        ///   <para>尝试读取数据</para>
        /// </summary>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">键</param>
        /// <param name="outValue">输出数据</param>
        [System.Obsolete("Please use async version for large data")]
        public static bool TryReadData<TData>(
            this IStorage self,
            string filePath,
            string key,
            out TData outValue,
            IStorage.DeserializerDelegate<TData> deserializer,
            TData defaultValue = default)
        {
            return self.TryReadData(filePath, key, out outValue, Encoding.UTF8, deserializer, defaultValue);
        }

        /// <summary>
        ///   <para>写入存储数据</para>
        /// </summary>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">键</param>
        /// <param name="value">数据</param>
        [System.Obsolete("Please use async version for large data")]
        public static void WriteData<TData>(
            this IStorage self,
            string filePath,
            string key,
            TData value,
            IStorage.SerializerDelegate serializer,
            IStorage.DeserializerDelegate<TData> deserializer)
        {
            self.WriteData(filePath, key, value, Encoding.UTF8, serializer, deserializer);
        }
        
        /// <summary>
        ///   <para>删除数据</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="key">键</param>
        public static void DeleteData(
            this IStorage self,
            string filePath, 
            string key,
            IStorage.DeserializerDelegate<object> deserializer)
        {
            self.DeleteData(filePath, key, Encoding.UTF8, deserializer);
        }
    }
}