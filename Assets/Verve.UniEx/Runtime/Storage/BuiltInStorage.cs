namespace VerveUniEx.Storage
{
    
#if UNITY_5_3_OR_NEWER
    using System;
    using UnityEngine;
    using Verve.Storage;
    using Verve.Serializable;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    
    
    public sealed partial class BuiltInStorage : StorageBase
    {
        private readonly ConcurrentDictionary<string, object> m_MemoryCache = new ConcurrentDictionary<string, object>();
        
        private SerializableUnit m_Unit;

        internal BuiltInStorage(SerializableUnit unit)
        {
            m_Unit = unit;
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
            outValue = m_Unit.Deserialize<JsonSerializableConverter, T>(PlayerPrefs.GetString(key, m_Unit.Serialize<JsonSerializableConverter>(defaultValue)));
            return outValue != null;
        }

        public void Write<T>(string key, T value) => Write(null, key, value);
        public override void Write<T>(string fileName, string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return;
            m_MemoryCache.AddOrUpdate(key, value, (s, _) => value);
            PlayerPrefs.SetString(key, m_Unit.Serialize<JsonSerializableConverter>(value));
        }

        public void Delete(string key) => Delete(null, key);
        public override void Delete(string fileName, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            m_MemoryCache.Remove(key, out _);
            PlayerPrefs.DeleteKey(key);
        }

        public override void DeleteAll()
        {
            m_MemoryCache.Clear();
            PlayerPrefs.DeleteAll();
        }
    }
#endif
    
}