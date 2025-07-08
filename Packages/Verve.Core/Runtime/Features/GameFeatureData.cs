namespace Verve
{
    using System;
    using System.Collections.Generic;

    
    /// <summary>
    /// 游戏功能数据基类
    /// </summary>
    [Serializable]
    public abstract class GameFeatureData : IGameFeatureData
    {
        public abstract string FeatureName { get; }
        public virtual IReadOnlyList<string> Dependencies { get; } = Array.Empty<string>();
        public virtual bool IsPersistent => true;
        public abstract IGameFeature CreateFeature();
    }
}