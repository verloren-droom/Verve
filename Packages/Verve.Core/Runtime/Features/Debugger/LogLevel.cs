namespace Verve.Debug
{
    /// <summary>
    ///   <para>日志等级</para>
    /// </summary>
    [System.Serializable]
    public enum LogLevel
    {
        /// <summary>
        ///   <para>错误</para>
        /// </summary>
        Error,
        
        /// <summary>
        ///   <para>警告</para>
        /// </summary>
        Warning,
        
        /// <summary>
        ///   <para>日志</para>
        /// </summary>
        Log,
        
        /// <summary>
        ///   <para>异常</para>
        /// </summary>
        Exception,
        
        /// <summary>
        ///   <para>断言</para>
        /// </summary>
        Assert,
    }
}