namespace Verve.MVC
{
    /// <summary>
    /// MVC功能数据
    /// </summary>
    [System.Serializable]
    public class MVCFeatureData : GameFeatureData 
    {
        public override string FeatureName => "Verve.MVC";
        
        
        public override IGameFeature CreateFeature()
        {
            return new MVCFeature();
        }
    }
}