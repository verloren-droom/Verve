namespace Verve.HotFix
{
    /// <summary>
    /// 热更新进度信息
    /// </summary>
    [System.Serializable]
    public class HotFixProgress
    {
        /// <summary>
        /// 当前的文件名
        /// </summary>
        public string CurrentFile { get; set; }
    
        /// <summary>
        /// 已处理的文件数
        /// </summary>
        public int ProcessedFiles { get; set; }
    
        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles { get; set; }
        
        /// <summary>
        /// 已传输的字节数
        /// </summary>
        public long BytesTransferred { get; set; }
    
        /// <summary>
        /// 总字节数
        /// </summary>
        public long TotalBytes { get; set; }
    }
}