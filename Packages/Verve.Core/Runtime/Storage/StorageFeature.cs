namespace Verve.Storage
{
    using File;
    using System;
    using Platform;
    using Serializable;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 持久化存储功能
    /// </summary>
    [Serializable]
    public class StorageFeature : ModularGameFeature
    {
        protected SerializableFeature m_Serializable;
        protected FileFeature m_File;
        protected PlatformFeature m_Platform;
        
        
        protected override void OnLoad()
        {
            base.OnLoad();
            m_Serializable = GameFeaturesSystem.Runtime.GetFeature<SerializableFeature>();
            m_File = GameFeaturesSystem.Runtime.GetFeature<FileFeature>();
            m_Platform = GameFeaturesSystem.Runtime.GetFeature<PlatformFeature>();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            
            RegisterSubmodule(new BinaryStorageSubmodule(m_Serializable));
            RegisterSubmodule(new JsonStorageSubmodule(m_Serializable, m_File, m_Platform));
        }

        public void Write<TStorage, TData>(string key, TData data) where TStorage : class, IStorageSubmodule =>
            Write<TStorage, TData>(null, key, data);
        public void Write<TStorage, TData>(string fileName, string key, TData data) where TStorage : class, IStorageSubmodule
        {
            GetSubmodule<TStorage>()?.Write(fileName, key, data);
        }

        public bool TryRead<TStorage, TData>(string key, out TData outValue, TData defaultValue = default)
            where TStorage : class, IStorageSubmodule => TryRead<TStorage, TData>(null, key, out outValue, defaultValue);
        public bool TryRead<TStorage, TData>(string fileName, string key, out TData outValue, TData defaultValue = default) where TStorage : class, IStorageSubmodule
        {
            return GetSubmodule<TStorage>().TryRead(fileName, key, out outValue, defaultValue);
        }
        
        public void Delete<TStorage>(string key) where TStorage : class, IStorageSubmodule => Delete<TStorage>(null, key);
        public void Delete<TStorage>(string fileName, string key)where TStorage : class, IStorageSubmodule
        {
            GetSubmodule<TStorage>()?.Delete(fileName, key);
        }

        public void DeleteAll<TStorage>() where TStorage : class, IStorageSubmodule => DeleteAll<TStorage>(null);
        public void DeleteAll<TStorage>(string fileName) where TStorage : class, IStorageSubmodule
        {
            GetSubmodule<TStorage>()?.DeleteAll(fileName);
        }

        public async Task WriteAsync<TStorage, TData>(string key, TData data) where TStorage : class, IStorageSubmodule =>
            await WriteAsync<TStorage, TData>(null, key, data);
        public async Task WriteAsync<TStorage, TData>(string fileName, string key, TData data) where TStorage : class, IStorageSubmodule
        {
            await GetSubmodule<TStorage>().WriteAsync(fileName, key, data);
        }
        
        public async Task<TData> ReadAsync<TStorage, TData>(string key, TData defaultValue = default) where TStorage : class, IStorageSubmodule =>
            await ReadAsync<TStorage, TData>(null, key, defaultValue);
        public async Task<TData> ReadAsync<TStorage, TData>(string fileName, string key, TData defaultValue = default) where TStorage : class, IStorageSubmodule
        {
            return await GetSubmodule<TStorage>().ReadAsync(fileName, key, defaultValue);
        }
    }
}