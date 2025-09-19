namespace Verve.Debug
{
    /// <summary>
    /// 日志数据
    /// </summary>
    [System.Serializable]
    public struct LastLogData
    {
        /// <summary> 日志消息 </summary>
        public string Message;
        /// <summary> 日志等级 </summary>
        public LogLevel Level;
    }
}