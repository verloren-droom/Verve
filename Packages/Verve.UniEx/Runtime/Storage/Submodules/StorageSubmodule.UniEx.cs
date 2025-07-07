#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Storage
{
    using System;
    using UnityEngine;
    using System.Collections;

    
    public abstract class StorageSubmodule : Verve.Storage.StorageSubmodule, VerveUniEx.Storage.IStorageSubmodule
    {
        public virtual IEnumerator ReadAsync<TData>(string fileName, string key, Action<TData> onComplete, TData defaultValue = default)
        {
            var task = ReadAsync(fileName, key, defaultValue);
            yield return new WaitUntil(() => task.IsCompleted);
            onComplete?.Invoke(task.Result);
        }

        public virtual IEnumerator WriteAsync<TData>(string fileName, string key, TData value, Action onComplete)
        {
            var task = WriteAsync(fileName, key, value);
            yield return new WaitUntil(() => task.IsCompleted);
            onComplete?.Invoke();
        }
    }
}

#endif