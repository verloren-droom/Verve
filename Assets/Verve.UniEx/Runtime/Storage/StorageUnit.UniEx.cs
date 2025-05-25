#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Storage
{
    using System;
    using File;
    using Verve.Unit;
    using System.Collections;
    using Verve.Serializable;
    
    
    /// <summary>
    /// 存储单元
    /// </summary>
    [CustomUnit("Storage", 0, typeof(SerializableUnit), typeof(FileUnit)), System.Serializable]
    public partial class StorageUnit : Verve.Storage.StorageUnit
    {
        protected FileUnit m_FileUnit;

        protected override void OnPostStartup(UnitRules parent)
        {
            parent.TryGetDependency(out m_Serializable);
            parent.TryGetDependency(out m_FileUnit);
            AddService(new JsonStorage(m_Serializable, m_FileUnit));
            AddService(new Verve.Storage.BinaryStorage(m_Serializable));
            AddService(new BuiltInStorage(m_Serializable));
        }

        public IEnumerator ReadAsync<TStorage, TData>(string fileName, string key, Action<TData> onComplete, TData defaultValue = default) 
            where TStorage : class, IStorage
        {
            yield return GetService<TStorage>().ReadAsync(fileName, key, onComplete, defaultValue);
        }
        
        public IEnumerator ReadAsync<TStorage, TData>(string key, Action<TData> onComplete, TData defaultValue = default)
            where TStorage : class, IStorage
        {
            yield return GetService<TStorage>().ReadAsync(null, key, onComplete, defaultValue);
        }

        public IEnumerator WriteAsync<TStorage, TData>(string fileName, string key, TData value, Action onComplete)
            where TStorage : class, IStorage
        {
            yield return GetService<TStorage>().WriteAsync(fileName, key, value, onComplete);
        }
        
        public IEnumerator WriteAsync<TStorage, TData>(string key, TData value, Action onComplete)
            where TStorage : class, IStorage
        {
            yield return GetService<TStorage>().WriteAsync(null, key, value, onComplete);
        }
    }
}
    
#endif