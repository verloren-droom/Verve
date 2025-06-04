namespace Verve.Loader
{
    using Unit;
    using System;
    using System.Threading.Tasks;


    /// <summary>
    /// 资源加载单元
    /// </summary>
    [CustomUnit("Loader", -1), System.Serializable]
    public partial class LoaderUnit : UnitBase<IAssetLoader>
    {
        public TAssetType LoadAsset<TLoaderType, TAssetType>(string assetPath) where TLoaderType : class, IAssetLoader => LoadAsset<TAssetType>(typeof(TLoaderType), assetPath);
        public TAssetType LoadAsset<TAssetType>(Type loaderType, string assetPath)
        {
            return GetService(loaderType).LoadAsset<TAssetType>(assetPath);
        }

        public async Task<TAssetType> LoadAssetsAsync<TLoaderType, TAssetType>(string assetPath) where TLoaderType : IAssetLoader => await LoadAssetAsync<TAssetType>(typeof(TLoaderType), assetPath);
        public async Task<TAssetType> LoadAssetAsync<TAssetType>(Type loaderType, string assetPath)
        {
            return await GetService(loaderType)?.LoadAssetAsync<TAssetType>(assetPath);
        }


        public void UnloadAsset<TLoaderType>(string assetPath) where TLoaderType : IAssetLoader => UnloadAsset(typeof(TLoaderType), assetPath);
        public void UnloadAsset(Type loaderType, string assetPath)
        {
            GetService(loaderType)?.UnloadAsset(assetPath);
        }

        public void UnloadAllAsset<TLoaderType>() where TLoaderType : IAssetLoader => UnloadAllAsset(typeof(TLoaderType));
        public void UnloadAllAsset(Type loaderType)
        {
            GetService(loaderType)?.UnloadAllAsset();
        }
    }
}