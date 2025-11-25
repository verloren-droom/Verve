namespace Verve.Debug
{
    /// <summary>
    ///   <para>日志数据</para>
    /// </summary>
    [System.Serializable]
    public struct LastLogData
    {
        /// <summary>
        ///   <para>日志消息</para>
        /// </summary>
        public string Message;
        
        /// <summary>
        ///   <para>日志等级</para>
        /// </summary>
        public LogLevel Level;
    }
}