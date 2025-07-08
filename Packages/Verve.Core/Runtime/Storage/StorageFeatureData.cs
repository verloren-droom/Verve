namespace Verve.Storage
{
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 存储功能数据
    /// </summary>
    [System.Serializable]
    public class StorageFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.Storage";

        public override IReadOnlyList<string> Dependencies => new string[] { "Verve.Serializable", "Verve.File", "Verve.Platform" };


        public override IGameFeature CreateFeature()
        {
            return new StorageFeature();
        }
    }
}