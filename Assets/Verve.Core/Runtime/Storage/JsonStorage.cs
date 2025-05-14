namespace Verve.Storage
{
    
    using File;
    using System;
    using System.IO;
    using Serializable;
    using System.Collections.Generic;

    
    public sealed partial class JsonStorage : StorageBase
    {
        private SerializableUnit m_Unit;

        private readonly Dictionary<string, byte[]> m_MemoryCache = new Dictionary<string, byte[]>();
        public string DefaultFileExtension { get; set; } = ".json";

        internal JsonStorage(SerializableUnit unit)
        {
            m_Unit = unit;
        }

        private string BuildSafePath(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName) ?? "default";
            return Path.Combine(FileDefine.BasePath, $"{safeFileName}{DefaultFileExtension}");
        }
        
        #region 核心API

        /// <summary>
        /// 写入数据
        /// </summary>
        public override void Write<T>(string fileName, string key, T data)
        {
            ValidateKey(key);
            var fullPath = BuildSafePath(fileName);
            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            var tempPath = Path.Combine(FileDefine.TempPath, Guid.NewGuid().ToString());

            try
            {
                Dictionary<string, object> dataDict = new Dictionary<string, object>();

                if (File.Exists(fullPath))
                {
                    try
                    {
                        dataDict = m_Unit.Deserialize<Dictionary<string, object>>(File.ReadAllBytes(fullPath));
                    }
                    catch { }
                }

                dataDict[key] = data;

                var bytes = m_Unit.Serialize(dataDict);
                
                m_MemoryCache[fullPath] = bytes;
                
                File.WriteAllBytes(File.Exists(fullPath) ? fullPath : tempPath, bytes);
                if (!File.Exists(fullPath))
                {
                    File.Move(tempPath, fullPath);
                }
            }
            catch (Exception e)
            {
                throw new StorageException("Data write failed", e);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        public override bool TryRead<T>(string fileName, string key, out T outValue, T defaultValue = default)
        {
            ValidateKey(key);
            var fullPath = BuildSafePath(fileName);

            try
            {
                if (!File.Exists(fullPath))
                {
                    outValue = defaultValue;
                    return false;
                }

                if (m_MemoryCache.TryGetValue(fullPath, out var bytes))
                {
                    outValue = GetValueFromCache(bytes, key, defaultValue);
                    return true;
                }

                bytes = File.ReadAllBytes(fullPath);
                m_MemoryCache[fullPath] = bytes;
                outValue = GetValueFromCache(bytes, key, defaultValue);
                return true;
            }
            catch
            {
                outValue = defaultValue;
                return false;
            }
        }

        #endregion

        #region 数据合并逻辑

        private T GetValueFromCache<T>(byte[] bytes, string key, T defaultValue)
        {
            try
            {
                var dataDict = m_Unit.Deserialize<Dictionary<string, object>>(bytes);
                if (dataDict.TryGetValue(key, out object value))
                {
                    return value is T typedValue ? typedValue : defaultValue;
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        #region 辅助方法

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty");

            if (key.Length > 256)
                throw new ArgumentException("Key exceeds maximum length (256)");
        }

        #endregion

        public void Delete(string key)
        {
            
        }
        
        /// <summary>
        /// 删除指定文件
        /// </summary>
        public override void Delete(string fileName, string key)
        {
            var fullPath = BuildSafePath(fileName);
            m_MemoryCache.Remove(fullPath);
            try
            {
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch { }
        }

        /// <summary>
        /// 清空所有缓存（内存+磁盘）
        /// </summary>
        public override void DeleteAll()
        {
            m_MemoryCache.Clear();

            if (Directory.Exists(FileDefine.BasePath))
            {
                foreach (var file in Directory.GetFiles(FileDefine.BasePath))
                {
                    try { File.Delete(file); }
                    catch { }
                }
            }
        }

        public bool TryRead<T>(string key, out T outValue, T defaultValue = default) => TryRead<T>(null, key, out outValue, defaultValue);

        public void Write<T>(string key, T value) => Write(null, key, value);
        
        public override void Dispose()
        {
            m_MemoryCache.Clear();
        }
    }
    
}