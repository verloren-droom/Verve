namespace Verve
{
    using System;
    using System.Diagnostics;

    
    /// <summary>
    ///   <para>调试器接口</para>
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        ///   <para>是否启用</para>
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        ///   <para>输出日志</para>
        /// </summary>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] void Log(object msg);
        
        /// <summary>
        ///   <para>输出日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough] void Log(string format, params object[] args);
        
        /// <summary>
        ///   <para>输出警告日志</para>
        /// </summary>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] void LogWarning(object msg);
        
        /// <summary>
        ///   <para>输出警告日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough] void LogWarning(string format, params object[] args);
        
        /// <summary>
        ///   <para>输出错误日志</para>
        /// </summary>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] void LogError(object msg);
        
        /// <summary>
        ///   <para>输出错误日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough] void LogError(string format, params object[] args);
        
        /// <summary>
        ///   <para>输出异常日志</para>
        /// </summary>
        /// <param name="exception">异常</param>
        [DebuggerHidden, DebuggerStepThrough] void LogException(Exception exception);
        
        /// <summary>
        ///   <para>断言</para>
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] void Assert(bool condition, object msg);
    }
}