namespace Verve.Timer
{
    /// <summary>
    /// 计时器功能数据
    /// </summary>
    [System.Serializable]
    public class TimerFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Timer";
        
        
        public override IGameFeature CreateFeature()
        {
            return new TimerFeature();
        }
    }
}