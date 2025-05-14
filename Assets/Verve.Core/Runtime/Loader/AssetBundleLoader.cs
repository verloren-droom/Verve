namespace Verve.Loader
{
    
#if UNITY_5_3_OR_NEWER
    using System;

    
    public sealed partial class AssetBundleLoader : AssetLoaderBase
    {
        public override TObject LoadAsset<TObject>(string assetPath)
        {
            throw new NotImplementedException();
        }

        public override void UnloadAsset<TObject>(TObject asset)
        {
            throw new NotImplementedException();
        }
    }
#endif
    
}