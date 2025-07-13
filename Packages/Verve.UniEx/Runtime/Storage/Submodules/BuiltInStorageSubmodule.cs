#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Storage
{
    using UnityEngine;
    using Verve.Serializable;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    
    
    /// <summary>
    /// Unity内置存储子模块
    /// </summary>
    public sealed partial class BuiltInStorageSubmodule : Verve.Storage.StorageSubmodule
    {
        public override string ModuleName => "BuiltInStorage";
        public override string DefaultFileExtension { get; set; } = ".json";

        private readonly ConcurrentDictionary<string, object> m_MemoryCache = new ConcurrentDictionary<string, object>();
        
        private SerializableFeature m_Serializable;

        internal BuiltInStorageSubmodule(SerializableFeature serializable)
        {
            m_Serializable = serializable;
        }

        public override void Dispose()
        {
            m_MemoryCache.Clear();
        }

        public bool TryRead<T>(string key, out T outValue, T defaultValue = default) =>
            TryRead(null, key, out outValue, defaultValue);
        public override bool TryRead<T>(string fileName, string key, out T outValue, T defaultValue = default)
        {
            if (m_MemoryCache.ContainsKey(key))
            {
                outValue = (T)m_MemoryCache[key];
                return true;
            }
            outValue = m_Serializable.GetSubmodule<JsonSerializableSubmodule>().Deserialize<T>(
                System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(key,
                    System.Text.Encoding.UTF8.GetString(m_Serializable.GetSubmodule<JsonSerializableSubmodule>()
                        .Serialize(defaultValue)))));
            return outValue != null;
        }

        public void Write<T>(string key, T value) => Write(null, key, value);
        public override void Write<T>(string fileName, string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return;
            m_MemoryCache.AddOrUpdate(key, value, (s, _) => value);
            PlayerPrefs.SetString(key, System.Text.Encoding.UTF8.GetString(m_Serializable.GetSubmodule<JsonSerializableSubmodule>().Serialize(value)));
        }

        public void Delete(string key) => Delete(null, key);
        public override void Delete(string fileName, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            m_MemoryCache.Remove(key, out _);
            PlayerPrefs.DeleteKey(key);
        }

        public override void DeleteAll(string fileName)
        {
            m_MemoryCache.Clear();
            PlayerPrefs.DeleteAll();
        }
    }
}
    
#endif