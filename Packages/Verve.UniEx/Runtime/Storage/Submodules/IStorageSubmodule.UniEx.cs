#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Storage
{
    using System;
    using System.Collections;
    
    
    public interface IStorageSubmodule : Verve.Storage.IStorageSubmodule
    {
        IEnumerator ReadAsync<TData>(string fileName, string key, Action<TData> onComplete, TData defaultValue = default);
        IEnumerator WriteAsync<TData>(string fileName, string key, TData value, Action onComplete);
    }
}

#endif