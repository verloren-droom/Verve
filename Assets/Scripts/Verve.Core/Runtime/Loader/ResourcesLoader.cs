namespace Verve.Loader
{
    using System;
    using UnityEngine;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    
    public sealed partial class ResourcesLoader : IAssetLoader
    {
        private readonly Dictionary<string, UnityEngine.Object> m_LoadedAssets = new Dictionary<string, UnityEngine.Object>();
        
        public TObject LoadAsset<TObject>(string assetPath)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(TObject)))
            {
                var asset = Resources.Load<UnityEngine.Object>(assetPath);
                m_LoadedAssets[assetPath] = asset;
                return (TObject)Convert.ChangeType(asset, typeof(TObject));
            }
            throw new TypeLoadException($"{typeof(TObject).Name} is not support!");
        }

        public async Task<TObject> LoadAssetAsync<TObject>(string assetPath)
        {
            return await Task.Run(() => LoadAsset<TObject>(assetPath));
        }

        public IEnumerator LoadAssetAsync<TObject>(string assetPath, Action<TObject> onComplete)
        {
            if (string.IsNullOrEmpty(assetPath)) yield return null;
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(TObject)))
            {
                var res = Resources.LoadAsync<UnityEngine.Object>(assetPath);
                res.completed += (_) =>
                {
                    m_LoadedAssets[assetPath] = res.asset;
                    onComplete?.Invoke((TObject)Convert.ChangeType(res.asset, typeof(TObject)));
                };
            }
            else
            {
                yield return null;
            }
        }

        public void ReleaseAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            if (m_LoadedAssets.TryGetValue(assetPath, out UnityEngine.Object asset))
            {
                m_LoadedAssets.Remove(assetPath);
                Resources.UnloadAsset(asset);
            }
        }

        public void ReleaseAsset<TObject>(TObject asset)
        {
            if (asset == null) return;
            if (asset is UnityEngine.Object unityAsset)
            {
                Resources.UnloadAsset(unityAsset);
            }
        }

        public void ReleaseAllAsset()
        {
            m_LoadedAssets.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}