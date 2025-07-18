namespace Verve
{
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 游戏功能元数据接口
    /// </summary>
    public interface IGameFeatureData
    {
        /// <summary> 功能名称 </summary>
        public string FeatureName { get; }
        /// <summary> 是否为常驻功能 </summary>
        public bool IsPersistent { get; }
        /// <summary> 功能依赖项 </summary>
        IReadOnlyCollection<string> Dependencies { get; }
        /// <summary> 创建功能实例 </summary>
        IGameFeature CreateFeature();
    }
}