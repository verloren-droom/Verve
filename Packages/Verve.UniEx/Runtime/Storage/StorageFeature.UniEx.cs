using UnityEngine;

#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Storage
{
    using System;
    using System.Collections;
    
    
    /// <summary>
    /// 存储功能
    /// </summary>
    [System.Serializable]
    public partial class StorageFeature : Verve.Storage.StorageFeature
    {
        protected override void OnLoad()
        {
            base.OnLoad();
            
            RegisterSubmodule(new BuiltInStorageSubmodule(m_Serializable));
        }

        public IEnumerator ReadAsync<TStorage, TData>(string fileName, string key, Action<TData> onComplete, TData defaultValue = default) 
            where TStorage : class, IStorageSubmodule
        {
            yield return GetSubmodule<TStorage>().ReadAsync(fileName, key, onComplete, defaultValue);
        }
        
        public IEnumerator ReadAsync<TStorage, TData>(string key, Action<TData> onComplete, TData defaultValue = default)
            where TStorage : class, IStorageSubmodule
        {
            yield return GetSubmodule<TStorage>().ReadAsync(null, key, onComplete, defaultValue);
        }

        public IEnumerator WriteAsync<TStorage, TData>(string fileName, string key, TData value, Action onComplete)
            where TStorage : class, IStorageSubmodule
        {
            yield return GetSubmodule<TStorage>().WriteAsync(fileName, key, value, onComplete);
        }
        
        public IEnumerator WriteAsync<TStorage, TData>(string key, TData value, Action onComplete)
            where TStorage : class, IStorageSubmodule
        {
            yield return GetSubmodule<TStorage>().WriteAsync(null, key, value, onComplete);
        }
    }
}

#endif