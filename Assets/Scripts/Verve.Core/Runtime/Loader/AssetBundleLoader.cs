namespace Verve.Loader
{
#if UNITY_5_3_OR_NEWER
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    
    
    public sealed partial class AssetBundleLoader : IAssetLoader
    {
        public TObject LoadAsset<TObject>(string assetPath)
        {
            throw new NotImplementedException();
        }

        public Task<TObject> LoadAssetAsync<TObject>(string assetPath)
        {
            throw new NotImplementedException();
        }

        public IEnumerator LoadAssetAsync<TObject>(string assetPath, Action<TObject> onComplete)
        {
            throw new NotImplementedException();
        }

        public void ReleaseAsset(string assetPath)
        {
            throw new NotImplementedException();
        }

        public void ReleaseAsset<TObject>(TObject asset)
        {
            throw new NotImplementedException();
        }

        public void ReleaseAllAsset()
        {
            throw new NotImplementedException();
        }
    }
#endif
}