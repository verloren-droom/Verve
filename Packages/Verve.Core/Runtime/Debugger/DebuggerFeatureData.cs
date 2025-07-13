namespace Verve.Debug
{
    /// <summary>
    /// 调试器功能数据
    /// </summary>
    [System.Serializable]
    public class DebuggerFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Debugger";
        
        
        public override IGameFeature CreateFeature()
        {
            return new DebuggerFeature();
        }
    }
}