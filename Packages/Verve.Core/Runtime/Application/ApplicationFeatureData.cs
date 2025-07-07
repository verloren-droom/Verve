namespace Verve.Application
{
    /// <summary>
    /// 应用功能数据
    /// </summary>
    [System.Serializable]
    public class ApplicationFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Application";
        
        
        public override IGameFeature CreateFeature()
        {
            return new ApplicationFeature();
        }
    }
}