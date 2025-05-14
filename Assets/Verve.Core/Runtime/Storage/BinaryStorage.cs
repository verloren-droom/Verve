namespace Verve.Storage
{
    
    using System;
    using Serializable;
    using System.Collections.Concurrent;
    
    
    public sealed partial class BinaryStorage : IStorage
    {
        public readonly ConcurrentDictionary<string, byte[]> m_MemoryCache = new ConcurrentDictionary<string, byte[]>();
        
        private SerializableUnit m_Unit;

        internal BinaryStorage(SerializableUnit unit)
        {
            m_Unit = unit;
        }

        public bool TryRead<T>(string key, out T outValue, T defaultValue = default) =>
            TryRead(null, key, out outValue, defaultValue);
        
        public bool TryRead<TData>(string fileName, string key, out TData outValue, TData defaultValue = default)
        {
            throw new NotImplementedException();
        }

        public void Write<T>(string key, T value) => Write(null, key, value);

        public void Write<T>(string fileName, string key, T value)
        {
            throw new NotImplementedException();
        }

        public void Delete(string key) => Delete(null, key);
        
        public void Delete(string fileName, string key)
        {
            throw new NotImplementedException();
        }
        
        public void DeleteAll()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            m_MemoryCache.Clear();
        }
    }
    
}