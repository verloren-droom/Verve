namespace Verve
{
    using System;
    using System.Collections.Generic;

    
    /// <summary>
    /// 游戏功能元数据基类
    /// </summary>
    [Serializable]
    public abstract class GameFeatureData : IGameFeatureData
    {
        /// <summary> 功能名称（默认为当前程序集名称） </summary>
        public virtual string FeatureName => GetType().Assembly.GetName().Name;
        public virtual IReadOnlyCollection<string> Dependencies { get; } = Array.Empty<string>();
        public virtual bool IsPersistent => true;
        public abstract IGameFeature CreateFeature();
    }
}