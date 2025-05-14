namespace Verve.Storage
{
    
    using Unit;
    using System;
    using Serializable;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 存储单元
    /// </summary>
    [CustomUnit("Storage", dependencyUnits: typeof(SerializableUnit)), System.Serializable]
    public partial class StorageUnit : UnitBase<IStorage>
    {
        protected SerializableUnit m_Serializable;

        private Dictionary<Type, IStorage> m_Storages = new Dictionary<Type, IStorage>();

        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);

            parent.onInitialized += rules =>
            {
                parent.TryGetDependency(out m_Serializable);
                Register(new JsonStorage(m_Serializable));
                Register(new BinaryStorage(m_Serializable));
            };
        }

        public void Write<TStorage, TData>(string key, TData data) where TStorage : IStorage =>
            Write<TStorage, TData>(null, key, data);
        public void Write<TStorage, TData>(string fileName, string key, TData data) where TStorage : IStorage
        {
            m_Storages?[typeof(TStorage)]?.Write(fileName, key, data);
        }

        public bool TryRead<TStorage, TData>(string key, out TData outValue, TData defaultValue = default)
            where TStorage : IStorage => TryRead<TStorage, TData>(null, key, out outValue, defaultValue);
        public bool TryRead<TStorage, TData>(string fileName, string key, out TData outValue, TData defaultValue = default) where TStorage : IStorage
        {
            return m_Storages[typeof(TStorage)].TryRead(fileName, key, out outValue, defaultValue);
        }
        
        public void Delete<TStorage>(string key) where TStorage : IStorage => Delete<TStorage>(null, key);
        public void Delete<TStorage>(string fileName, string key)where TStorage : IStorage
        {
            m_Storages?[typeof(TStorage)]?.Delete(fileName, key);
        }

        public void DeleteAll<TStorage>() where TStorage : IStorage
        {
            m_Storages?[typeof(TStorage)]?.DeleteAll();
        }
    }
}