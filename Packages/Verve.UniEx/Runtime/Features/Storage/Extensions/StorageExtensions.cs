#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Storage
{
    using UnityEngine;
    using System.Text;
    using Verve.Storage;


    /// <summary>
    /// 存储扩展类，用于简化存储操作
    /// </summary>
    public static class StorageExtensions
    {
        /// <summary>
        /// 尝试读取数据
        /// </summary>
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
        /// 读取存储数据（默认使用 UnityEngine.JsonUtility 序列化/反序列化功能）
        /// </summary>
        [System.Obsolete("Please use async version for large data")]
        public static bool TryReadData<TData>(
            this IStorage self,
            string filePath,
            string key,
            out TData outValue,
            Encoding encoding,
            TData defaultValue = default)
        {
            return self.TryReadData(filePath, key, out outValue, (data) => JsonUtility.FromJson<TData>(encoding.GetString(data)), defaultValue);
        }
        
        /// <summary>
        /// 读取存储数据（默认使用 UnityEngine.JsonUtility 序列化/反序列化功能）
        /// </summary>
        [System.Obsolete("Please use async version for large data")]
        public static bool TryReadData<TData>(
            this IStorage self,
            string filePath,
            string key,
            out TData outValue,
            TData defaultValue = default)
        {
            return self.TryReadData(filePath, key, out outValue, Encoding.UTF8, defaultValue);
        }
        
        /// <summary>
        /// 写入存储数据
        /// </summary>
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
        /// 写入存储数据（默认使用 UnityEngine.JsonUtility 序列化/反序列化功能）
        /// </summary>
        [System.Obsolete("Please use async version for large data")]
        public static void WriteData<TData>(
            this IStorage self,
            string filePath,
            string key,
            TData value,
            Encoding encoding)
        {
            self.WriteData(filePath, key, value, (data) => encoding.GetBytes(JsonUtility.ToJson(data)), (data) => JsonUtility.FromJson<TData>(encoding.GetString(data)));
        }

        /// <summary>
        /// 写入存储数据（默认使用 UnityEngine.JsonUtility 序列化/反序列化功能）
        /// </summary>
        [System.Obsolete("Please use async version for large data")]
        public static void WriteData<TData>(
            this IStorage self,
            string filePath,
            string key,
            TData value)
        {
            self.WriteData(filePath, key, value, Encoding.UTF8);
        }
        
        /// <summary>
        /// 删除数据
        /// </summary>
        public static void DeleteData(
            this IStorage self,
            string filePath, 
            string key,
            IStorage.DeserializerDelegate<object> deserializer)
        {
            self.DeleteData(filePath, key, Encoding.UTF8, deserializer);
        }
        
        /// <summary>
        /// 删除存储数据（默认使用 UnityEngine.JsonUtility 序列化/反序列化功能）
        /// </summary>
        public static void DeleteData(
            this IStorage self,
            string filePath, 
            string key,
            Encoding encoding)
        {
            self.DeleteData(filePath, key, encoding, (data) => JsonUtility.FromJson<object>(encoding.GetString(data)));
        }
        
        /// <summary>
        /// 删除存储数据（默认使用UnityEngine.JsonUtility序列化/反序列化功能）
        /// </summary>
        public static void DeleteData(
            this IStorage self,
            string filePath, 
            string key)
        {
            self.DeleteData(filePath, key, Encoding.UTF8);
        }
    }
}

#endif