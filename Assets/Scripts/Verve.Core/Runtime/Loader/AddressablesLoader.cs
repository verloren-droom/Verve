namespace Verve.Loader
{
#if UNITY_2018_3_OR_NEWER
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    
    
    [System.Serializable]
    public sealed partial class AddressablesLoader : IAssetLoader
    {
        private readonly Dictionary<string, AsyncOperationHandle> m_Handles = new Dictionary<string, AsyncOperationHandle>();
        
        
        public TObject LoadAsset<TObject>(string assetPath)
        {
            return TrackHandle(Addressables.LoadAssetAsync<TObject>(assetPath), assetPath).WaitForCompletion();
        }
        
        public async Task<TObject> LoadAssetAsync<TObject>(string assetPath)
        {
            return await TrackHandle(Addressables.LoadAssetAsync<TObject>(assetPath), assetPath).Task;
        }

        public IEnumerator LoadAssetAsync<TObject>(string assetPath, Action<TObject> onComplete)
        {
            if (string.IsNullOrEmpty(assetPath)) yield return null;
            var handle = TrackHandle(Addressables.LoadAssetAsync<TObject>(assetPath), assetPath);
            while (!handle.IsDone)
            {
                yield return null;
            }
            onComplete?.Invoke(handle.Result);
        }

        public void ReleaseAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            if (m_Handles.TryGetValue(assetPath, out var handle))
            {
                Addressables.Release(handle);
                m_Handles.Remove(assetPath);
            }
        }

        public void ReleaseAsset<TObject>(TObject asset)
        {
            if (asset == null) return;
            Addressables.Release(asset);
        }

        private AsyncOperationHandle<TObject> TrackHandle<TObject>(AsyncOperationHandle<TObject> newHandle, string assetPath)
        {
            if (m_Handles.TryGetValue(assetPath, out var existingHandle))
            {
                if (newHandle.IsValid() && !newHandle.IsDone)
                {
                    Addressables.Release(newHandle);
                }
                if (existingHandle is AsyncOperationHandle<TObject> typedHandle)
                {
                    return typedHandle;
                }
                Addressables.Release(existingHandle);
                m_Handles.Remove(assetPath);
            }
            m_Handles[assetPath] = newHandle;
            return newHandle;
        }

        public void ReleaseAllAsset()
        {
            foreach (var handle in m_Handles.Values)
            {
                Addressables.Release(handle);
            }
            m_Handles.Clear();
        }
    }
#endif
}