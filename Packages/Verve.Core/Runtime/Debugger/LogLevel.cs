namespace Verve.Debug
{
    /// <summary>
    /// 日志等级
    /// </summary>
    [System.Serializable]
    public enum LogLevel
    {
        /// <summary> 错误 </summary>
        Error,
        /// <summary> 警告 </summary>
        Warning,
        /// <summary> 日志 </summary>
        Log,
        /// <summary> 异常 </summary>
        Exception,
        /// <summary> 断言 </summary>
        Assert,
    }
}