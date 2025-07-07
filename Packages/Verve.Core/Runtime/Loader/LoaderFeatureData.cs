namespace Verve.Loader
{
    /// <summary>
    /// 加载器功能数据
    /// </summary>
    [System.Serializable]
    public class LoaderFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Loader";
        
        
        public override IGameFeature CreateFeature()
        {
            return new LoaderFeature();
        }
    }
}