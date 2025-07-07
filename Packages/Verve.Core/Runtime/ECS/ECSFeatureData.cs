namespace Verve.ECS
{
    /// <summary>
    /// ECS功能数据
    /// </summary>
    [System.Serializable]
    public class ECSFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.ECS";
        
        
        public override IGameFeature CreateFeature()
        {
            return new ECSFeature();
        }
    }
}