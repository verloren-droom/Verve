namespace Verve.Storage
{
    public interface IStorage : System.IDisposable
    {
        /// <summary>
        /// 尝试读取数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="key"></param>
        /// <param name="outValue">输出数据</param>
        /// <param name="defaultValue">默认值</param>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <returns></returns>
        bool TryRead<TData>(string fileName, string key, out TData outValue, TData defaultValue = default);
        
        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="key"></param>
        /// <param name="value">存入数据</param>
        /// <typeparam name="TData">数据类型</typeparam>
        void Write<TData>(string fileName, string key, TData value);
        
        /// <summary>
        /// 删除指定数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="key"></param>
        void Delete(string fileName, string key);

        /// <summary>
        /// 删除所有数据
        /// </summary>
        void DeleteAll();
    }
}