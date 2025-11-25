#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Storage
{
    using System;
    using System.IO;
    using UnityEngine;
    using System.Text;
    using Verve.Storage;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    
    /// <summary>
    ///   <para>JSON文件存储</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(StorageGameFeature), Description = "JSON文件存储")]
    public sealed partial class JsonStorage : StorageSubmodule
    {
        public override string DefaultFileExtension { get; set; } = ".json";
        private readonly ConcurrentDictionary<string, object> m_MemoryCache = new ConcurrentDictionary<string, object>();
        

        public override bool TryReadData<TData>(
            string filePath,
            string key,
            out TData outValue,
            Encoding encoding,
            IStorage.DeserializerDelegate<TData> deserializer,
            TData defaultValue = default)
        {
            outValue = defaultValue;
            
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }
            
            string cacheKey = GetCacheKey(filePath, key);
            if (m_MemoryCache.TryGetValue(cacheKey, out object cachedValue) && cachedValue is TData typedValue)
            {
                outValue = typedValue;
                return true;
            }
            
            string fullPath = BuildFullPath(filePath);
            
            if (!File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                byte[] jsonBytes = File.ReadAllBytes(fullPath);
                
                IStorage.DeserializerDelegate<Dictionary<string, object>> dictDeserializer = (data) => {
                    if (deserializer != null && typeof(TData) == typeof(Dictionary<string, object>))
                    {
                        return (Dictionary<string, object>)(object)deserializer(data);
                    }

                    return new Dictionary<string, object>();
                };
                
                Dictionary<string, object> fileData = dictDeserializer(jsonBytes);

                if (fileData != null && fileData.TryGetValue(key, out var value) && value is TData result)
                {
                    outValue = result;
                    
                    m_MemoryCache[cacheKey] = outValue;

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to read data from {fullPath}: {ex.Message}");
            }
            
            return false;
        }

        public override void WriteData<TData>(
            string filePath,
            string key,
            TData value,
            Encoding encoding,
            IStorage.SerializerDelegate serializer,
            IStorage.DeserializerDelegate<TData> deserializer)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            
            string fullPath = BuildFullPath(filePath);
            var fileData = new Dictionary<string, object>();
            
            if (File.Exists(fullPath))
            {
                try
                {
                    byte[] jsonBytes = File.ReadAllBytes(fullPath);
                    fileData = (Dictionary<string, object>)(object)deserializer(jsonBytes);
                }
                catch
                {
                    fileData = new Dictionary<string, object>();
                }
            }
            
            fileData[key] = value;
            
            try
            {
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                byte[] jsonData = serializer(fileData);
                File.WriteAllBytes(fullPath, jsonData);
                
                string cacheKey = GetCacheKey(filePath, key);
                m_MemoryCache[cacheKey] = value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write data to {fullPath}: {ex.Message}");
            }
        }

        public override void DeleteData<TData>(
            string filePath, 
            string key, 
            Encoding encoding,
            IStorage.DeserializerDelegate<TData> deserializer)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("File path cannot be null or empty");
                return;
            }
            
            string fullPath = BuildFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                return;
            }
            
            try
            {
                byte[] jsonBytes = File.ReadAllBytes(fullPath);
                Dictionary<string, object> fileData = (Dictionary<string, object>)(object)deserializer(jsonBytes);
                
                if (fileData != null && fileData.TryGetValue(key, out object value) && value is TData result)
                {
                    fileData.Remove(key);
                    
                    if (fileData.Count == 0)
                    {
                        File.Delete(fullPath);
                    }
                    else
                    {
                        // byte[] jsonData = SerializeDictionary(fileData, null);
                        // File.WriteAllBytes(fullPath, jsonData);
                    }
                    
                    string cacheKey = GetCacheKey(filePath, key);
                    m_MemoryCache.TryRemove(cacheKey, out _);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete data from {fullPath}: {ex.Message}");
            }
        }

        public override void DeleteAllData(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("File path cannot be null or empty");
                return;
            }
            
            string fullPath = BuildFullPath(filePath);
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    
                    ClearFileCache(filePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to delete file {fullPath}: {ex.Message}");
                }
            }
        }

        public override bool HasData(string filePath, string key, IStorage.DeserializerDelegate<object> deserializer)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }
            
            string fullPath = BuildFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                return false;
            }
            
            try
            {
                string cacheKey = GetCacheKey(filePath, key);
                if (m_MemoryCache.ContainsKey(cacheKey))
                {
                    return true;
                }
                
                byte[] jsonBytes = File.ReadAllBytes(fullPath);
                var fileData = (Dictionary<string, object>)deserializer(jsonBytes);
                return fileData != null && fileData.ContainsKey(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to check data existence in {filePath}: {ex.Message}");
                return false;
            }
        }

        private string BuildFullPath(string filePath = null)
        {
            string safeFileName = filePath ?? "default";
            
            if (!Path.HasExtension(safeFileName))
            {
                safeFileName = $"{safeFileName}{DefaultFileExtension}";
            }

            if (Path.IsPathRooted(safeFileName))
            {
                return safeFileName;
            }
            
            return Path.Combine(Application.persistentDataPath, safeFileName);
        }

        private string GetCacheKey(string filePath, string key)
        {
            return $"{filePath}:{key}";
        }
        
        private void ClearFileCache(string filePath)
        {
            foreach (var cacheKey in m_MemoryCache.Keys)
            {
                if (cacheKey.StartsWith($"{filePath}:"))
                {
                    m_MemoryCache.TryRemove(cacheKey, out _);
                }
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_MemoryCache.Clear();
            }
        }
    }
}

#endif
