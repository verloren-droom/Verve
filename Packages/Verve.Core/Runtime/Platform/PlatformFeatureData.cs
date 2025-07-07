namespace Verve.Platform
{
    /// <summary>
    /// 平台功能数据
    /// </summary>
    [System.Serializable]
    public class PlatformFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Platform";
        
        
        public override IGameFeature CreateFeature()
        {
            return new PlatformFeature();
        }
    }
}