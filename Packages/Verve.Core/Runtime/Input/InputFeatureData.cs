namespace Verve.Input
{
    /// <summary>
    /// 输入功能数据
    /// </summary>
    [System.Serializable]
    public class InputFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Input";
        

        public override IGameFeature CreateFeature()
        {
            return new InputFeature();
        }
    }
}