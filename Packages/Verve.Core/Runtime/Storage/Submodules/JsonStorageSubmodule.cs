namespace Verve.Storage
{
    using File;
    using System;
    using Platform;
    using System.IO;
    using Serializable;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    
    /// <summary>
    /// JSON存储子模块
    /// </summary>
    [Serializable]
    public partial class JsonStorageSubmodule : StorageSubmodule
    {
        public override string ModuleName => "JsonStorage";
        
        private readonly SerializableFeature m_SerializableUnit;
        private readonly FileFeature m_File;
        private readonly PlatformFeature m_Platform;
        
        private readonly ConcurrentDictionary<string, object> m_MemoryCache = new ConcurrentDictionary<string, object>();
        
        public override string DefaultFileExtension { get; set; } = ".json";

        protected internal JsonStorageSubmodule(SerializableFeature serializable, FileFeature file, PlatformFeature platform)
        {
            m_File = file ?? throw new ArgumentNullException(nameof(file));
            m_SerializableUnit = serializable ?? throw new ArgumentNullException(nameof(serializable));
            m_Platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }

        public override void Write<T>(string fileName, string key, T value)
        {
            ValidateParameters(fileName, key);
            
            var fullPath = BuildFullPath(fileName);
            var dataDict = GetOrCreateFileData(fullPath);
            dataDict[key] = value;
            
            SaveFileData(fullPath, dataDict);
            CacheData(fullPath, key, value);
        }

        public override bool TryRead<T>(string fileName, string key, out T value, T defaultValue = default)
        {
            value = defaultValue;
            if (!ValidateParameters(fileName, key)) return false;
            
            var fullPath = BuildFullPath(fileName);
            if (TryGetFromCache(fullPath, key, out value)) return true;
            
            var dataDict = LoadFileData(fullPath);
            if (dataDict == null || !dataDict.TryGetValue(key, out object rawValue))
                return false;
                
            value = rawValue is T typedValue ? typedValue : defaultValue;
            CacheData(fullPath, key, value);
            return true;
        }

        public override void Delete(string fileName, string key)
        {
            if (!ValidateParameters(fileName, key)) return;
            
            var fullPath = BuildFullPath(fileName);
            var dataDict = LoadFileData(fullPath);
            if (dataDict?.Remove(key) ?? false)
            {
                SaveFileData(fullPath, dataDict);
                RemoveCache(fullPath, key);
            }
        }

        public override void DeleteAll(string fileName)
        {
            var fullPath = BuildFullPath(fileName);
            m_File.DeleteFile(fullPath);
            ClearFileCache(fullPath);
        }

        private string BuildFullPath(string fileName)
        {
            var safeFileName = fileName ?? "default";
            return m_File.GetFullFilePath(Path.HasExtension(safeFileName) ? safeFileName : $"{safeFileName}{DefaultFileExtension}");
        }

        private Dictionary<string, object> GetOrCreateFileData(string fullPath)
        {
            return LoadFileData(fullPath) ?? new Dictionary<string, object>();
        }

        private Dictionary<string, object> LoadFileData(string fullPath)
        {
            if (!File.Exists(fullPath)) return null;
            
            try
            {
                using var fs = File.OpenRead(fullPath);
                return m_SerializableUnit.DeserializeFromStream<JsonSerializableSubmodule, Dictionary<string, object>>(fs);
            }
            catch
            {
                return null;
            }
        }

        private void SaveFileData(string fullPath, Dictionary<string, object> dataDict)
        {
            var relativePath = Path.GetRelativePath(m_Platform.GetPersistentDataPath(), fullPath);
            m_File.WriteFile<JsonSerializableSubmodule, Dictionary<string, object>>(relativePath, dataDict);
        }

        private bool TryGetFromCache<T>(string fullPath, string key, out T value)
        {
            var cacheKey = $"{fullPath}:{key}";
            if (m_MemoryCache.TryGetValue(cacheKey, out object cached) && cached is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        private void CacheData<T>(string fullPath, string key, T value)
        {
            var cacheKey = $"{fullPath}:{key}";
            m_MemoryCache.AddOrUpdate(cacheKey, value, (_, __) => value);
        }

        private void RemoveCache(string fullPath, string key)
        {
            var cacheKey = $"{fullPath}:{key}";
            m_MemoryCache.TryRemove(cacheKey, out _);
        }

        private void ClearFileCache(string fullPath)
        {
            foreach (var key in m_MemoryCache.Keys)
            {
                if (key.StartsWith(fullPath))
                {
                    m_MemoryCache.TryRemove(key, out _);
                }
            }
        }

        private static bool ValidateParameters(string fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (key.Length > 256)
                throw new ArgumentException("Key exceeds maximum length (256)");

            return true;
        }

        public override void Dispose() => m_MemoryCache.Clear();
    }
}