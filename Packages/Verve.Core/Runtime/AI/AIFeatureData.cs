namespace Verve.AI
{
    /// <summary>
    /// AI功能数据
    /// </summary>
    [System.Serializable]
    public class AIFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.AI";
        
        
        public override IGameFeature CreateFeature()
        {
            return new AIFeature();
        }
    }
}