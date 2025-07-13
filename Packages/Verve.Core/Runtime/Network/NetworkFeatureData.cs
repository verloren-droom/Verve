namespace Verve.Net
{
    /// <summary>
    /// 网络功能数据
    /// </summary>
    [System.Serializable]
    public class NetworkFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Net";
        
        
        public override IGameFeature CreateFeature() => new NetworkFeature();
    }
}