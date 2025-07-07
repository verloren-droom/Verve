namespace Verve.Event
{
    /// <summary>
    /// 事件总线功能数据
    /// </summary>
    [System.Serializable]
    public class EventBusFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Event";
        
        
        public override IGameFeature CreateFeature()
        {
            return new EventBusFeature();
        }
    }
}