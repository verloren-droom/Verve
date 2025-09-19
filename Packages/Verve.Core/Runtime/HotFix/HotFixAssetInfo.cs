namespace Verve.HotFix
{
    /// <summary>
    /// 热更新资源信息
    /// </summary>
    [System.Serializable]
    public class HotFixAssetInfo
    {
        /// <summary>
        /// 远程地址
        /// </summary>
        public string RemoteUrl { get; set; }
        /// <summary>
        /// 本地地址
        /// </summary>
        public string LocalPath { get; set; }
        /// <summary>
        /// 校验码
        /// </summary>
        public string Checksum { get; set; }
        /// <summary>
        /// 文件大小
        /// </summary>
        public long Size { get; set; }
    }
}