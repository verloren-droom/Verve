namespace Verve.Loader
{
    using System;
    using System.Threading.Tasks;
    
    
    /// <summary>
    /// 加载器功能
    /// </summary>
    [Serializable]
    public class LoaderFeature : ModularGameFeature
    {
        public TAssetType LoadAsset<TLoaderType, TAssetType>(string assetPath) where TLoaderType : class, IAssetLoader => LoadAsset<TAssetType>(typeof(TLoaderType), assetPath);
        public TAssetType LoadAsset<TAssetType>(Type loaderType, string assetPath)
        {
            return ((IAssetLoader)GetSubmodule(loaderType)).LoadAsset<TAssetType>(assetPath);
        }

        public async Task<TAssetType> LoadAssetsAsync<TLoaderType, TAssetType>(string assetPath) where TLoaderType : IAssetLoader => await LoadAssetAsync<TAssetType>(typeof(TLoaderType), assetPath);
        public async Task<TAssetType> LoadAssetAsync<TAssetType>(Type loaderType, string assetPath)
        {
            return await ((IAssetLoader)GetSubmodule(loaderType)).LoadAssetAsync<TAssetType>(assetPath);
        }


        public void UnloadAsset<TLoaderType>(string assetPath) where TLoaderType : IAssetLoader => UnloadAsset(typeof(TLoaderType), assetPath);
        public void UnloadAsset(Type loaderType, string assetPath)
        {
            ((IAssetLoader)GetSubmodule(loaderType))?.UnloadAsset(assetPath);
        }

        public void UnloadAllAsset<TLoaderType>() where TLoaderType : IAssetLoader => UnloadAllAsset(typeof(TLoaderType));
        public void UnloadAllAsset(Type loaderType)
        {
            ((IAssetLoader)GetSubmodule(loaderType))?.UnloadAllAsset();
        }
    }
}