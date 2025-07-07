namespace Verve.Serializable
{
    /// <summary>
    /// 序列化功能数据
    /// </summary>
    [System.Serializable]
    public class SerializableFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Serializable";
        
        
        public override IGameFeature CreateFeature()
        {
            return new SerializableFeature();
        }
    }
}