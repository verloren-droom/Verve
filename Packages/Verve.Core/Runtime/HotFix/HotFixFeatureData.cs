namespace Verve.HotFix
{
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 热更新功能数据
    /// </summary>
    [System.Serializable]
    public class HotFixFeatureData : GameFeatureData
    {
        /// <summary> 服务器地址 </summary>
        public const string ServerUrl = "http://localhost:8080";
        /// <summary>  热更清单默认文件名 </summary>
        public const string ManifestName = "manifest.json";

        public override string FeatureName => "Verve.HotFix";
        
        public override IReadOnlyCollection<string> Dependencies => new string[] { "Verve.Loader", "Verve.Net", "Verve.Platform" };
        
        public override IGameFeature CreateFeature() => new HotFixFeature();
    }
}