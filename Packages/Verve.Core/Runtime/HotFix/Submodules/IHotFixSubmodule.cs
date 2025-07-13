namespace Verve.HotFix
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    
    /// <summary>
    /// 热更新子模块
    /// </summary>
    public interface IHotFixSubmodule : IGameFeatureSubmodule
    {
        /// <summary> 检查并获取热更清单 </summary>
        Task<HotFixManifest> CheckForUpdatesAsync(string checksum);
        
        /// <summary> 异步应用热更新 </summary>
        Task ApplyUpdateAsync(string checksum, Version targetVersion = null);
    }
}
