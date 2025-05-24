namespace Verve.Storage
{
    using Unit;
    using File;
    using System;
    using Serializable;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 存储单元
    /// </summary>
    [CustomUnit("Storage", dependencyUnits: typeof(SerializableUnit)), System.Serializable]
    public partial class StorageUnit : UnitBase<IStorage>
    {
        protected SerializableUnit m_Serializable;
        protected FileUnit m_FileUnit;

        
        protected override void OnPostStartup(UnitRules parent)
        {
            base.OnPostStartup(parent);
            parent.TryGetDependency(out m_Serializable);
            parent.TryGetDependency(out m_FileUnit);
            AddService(new JsonStorage(m_Serializable, m_FileUnit));
            AddService(new BinaryStorage(m_Serializable));
        }

        public void Write<TStorage, TData>(string key, TData data) where TStorage : class, IStorage =>
            Write<TStorage, TData>(null, key, data);
        public void Write<TStorage, TData>(string fileName, string key, TData data) where TStorage : class, IStorage
        {
            GetService<TStorage>()?.Write(fileName, key, data);
        }

        public bool TryRead<TStorage, TData>(string key, out TData outValue, TData defaultValue = default)
            where TStorage : class, IStorage => TryRead<TStorage, TData>(null, key, out outValue, defaultValue);
        public bool TryRead<TStorage, TData>(string fileName, string key, out TData outValue, TData defaultValue = default) where TStorage : class, IStorage
        {
            return GetService<TStorage>().TryRead(fileName, key, out outValue, defaultValue);
        }
        
        public void Delete<TStorage>(string key) where TStorage : class, IStorage => Delete<TStorage>(null, key);
        public void Delete<TStorage>(string fileName, string key)where TStorage : class, IStorage
        {
            GetService<TStorage>()?.Delete(fileName, key);
        }

        public void DeleteAll<TStorage>() where TStorage : class, IStorage
        {
            GetService<TStorage>()?.DeleteAll();
        }
        
        public async Task WriteAsync<TStorage, TData>(string key, TData data) where TStorage : class, IStorage =>
            await WriteAsync<TStorage, TData>(null, key, data);
        public async Task WriteAsync<TStorage, TData>(string fileName, string key, TData data) where TStorage : class, IStorage
        {
            await GetService<TStorage>().WriteAsync(fileName, key, data);
        }
        
        public async Task<TData> ReadAsync<TStorage, TData>(string key, TData defaultValue = default) where TStorage : class, IStorage =>
            await ReadAsync<TStorage, TData>(null, key, defaultValue);
        public async Task<TData> ReadAsync<TStorage, TData>(string fileName, string key, TData defaultValue = default) where TStorage : class, IStorage
        {
            return await GetService<TStorage>().ReadAsync(fileName, key, defaultValue);
        }
    }
}