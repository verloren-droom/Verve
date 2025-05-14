namespace Verve.Loader
{
    
    using System.Threading.Tasks;
    
    
    /// <summary>
    /// 资源加载基类
    /// </summary>
    public abstract class AssetLoaderBase : IAssetLoader
    {
        public abstract TObject LoadAsset<TObject>(string assetPath);
        public virtual async Task<TObject> LoadAssetAsync<TObject>(string assetPath) => await Task.Run(() => LoadAsset<TObject>(assetPath));
        public virtual void UnloadAsset(string assetPath) {}
        public abstract void UnloadAsset<TObject>(TObject asset);
        public virtual void UnloadAllAsset() {}
        public virtual void Dispose() {}
    }
    
}