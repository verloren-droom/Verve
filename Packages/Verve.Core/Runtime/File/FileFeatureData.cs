namespace Verve.File
{
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 文件功能数据
    /// </summary>
    [System.Serializable]
    public partial class FileFeatureData : GameFeatureData
    {
        public override string FeatureName => "Verve.File";

        public override IReadOnlyCollection<string> Dependencies => new string[] { "Verve.Serializable", "Verve.Platform" };
        
        
        public override IGameFeature CreateFeature()
        {
            return new FileFeature();
        }
    }
}