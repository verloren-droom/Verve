namespace Verve.HotFix
{
    using System;
    using System.Collections.Generic;
    

    /// <summary>
    /// 更新清单数据
    /// </summary>
    [Serializable]
    public class HotFixManifest
    {
        /// <summary> 版本 </summary>
        public Version Version;

        /// <summary> 更新描述 </summary>
        public string Description;

        /// <summary> 是否强制更新 </summary>
        public bool IsMandatory = true;

        /// <summary> 热更新资源 </summary>
        public Dictionary<string, HotFixAssetInfo> Assets = new Dictionary<string, HotFixAssetInfo>();
    }
    
    
    /// <summary>
    /// 热更新资源信息
    /// </summary>
    [Serializable]
    public class HotFixAssetInfo
    {
        public string RemoteUrl { get; set; }
        public string Checksum { get; set; }
        public long Size { get; set; }
    }
}