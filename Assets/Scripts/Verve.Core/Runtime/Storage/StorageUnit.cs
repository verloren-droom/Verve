#if !VERVE_FRAMEWORK_STORAGE
#define VERVE_FRAMEWORK_STORAGE
#endif


namespace Verve.Storage
{
    using Unit;
    using System;
    using Serializable;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 存储单元
    /// </summary>
    [CustomUnit("Storage"), System.Serializable]
    public sealed partial class StorageUnit : UnitBase
    {
        public override HashSet<Type> DependencyUnits { get; protected set; } = new HashSet<Type>()
        {
            typeof(SerializableUnit)
        };
        private SerializableUnit m_Unit;

        private Dictionary<Type, IStorage> m_Storages = new Dictionary<Type, IStorage>();

        public override void Startup(UnitRules parent, params object[] args)
        {
            base.Startup(parent, args);

            parent.onInitialized += rules =>
            {
                parent.TryGetDependency(out m_Unit);
                
#if UNITY_5_3_OR_NEWER
                m_Storages.Add(typeof(BuiltInStorage), new BuiltInStorage(m_Unit));
#endif
                m_Storages.Add(typeof(JsonStorage), new JsonStorage(m_Unit));
                m_Storages.Add(typeof(BinaryStorage), new BinaryStorage(m_Unit));
            };
        }

        public void Write<TStorage, TData>(string fileName, string key, TData data) where TStorage : IStorage
        {
            m_Storages?[typeof(TStorage)]?.Write(fileName, key, data);
        }

        public void Write<TStorage, TData>(string key, TData data) where TStorage : IStorage =>
            Write<TStorage, TData>(null, key, data);

        
        public bool TryRead<TStorage, TData>(string fileName, string key, out TData outValue, TData defaultValue = default) where TStorage : IStorage
        {
            return m_Storages[typeof(TStorage)].TryRead(fileName, key, out outValue, defaultValue);
        }

        public bool TryRead<TStorage, TData>(string key, out TData outValue, TData defaultValue = default)
            where TStorage : IStorage => TryRead<TStorage, TData>(null, key, out outValue, defaultValue);

        
        public void Delete<TStorage>(string fileName, string key)where TStorage : IStorage
        {
            m_Storages?[typeof(TStorage)]?.Delete(fileName, key);
        }

        public void Delete<TStorage>(string key) where TStorage : IStorage => Delete<TStorage>(null, key);
        
        public void DeleteAll<TStorage>() where TStorage : IStorage
        {
            m_Storages?[typeof(TStorage)]?.DeleteAll();
        }
    }
    
    public class StorageException : Exception
    {
        public StorageException(string message, Exception inner) : base(message, inner) { }
    }
}