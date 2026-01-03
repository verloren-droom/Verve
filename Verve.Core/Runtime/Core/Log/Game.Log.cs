namespace Verve
{
    using System;
    using System.Diagnostics;
    
    
    /// <summary>
    ///   <para>游戏入口：日志部分</para>
    /// </summary>
    public static partial class Game
    {
        private static ILogger s_Logger = Logger.Instance;
        
        /// <summary>
        ///   <para>设置日志系统</para>
        /// </summary>
        /// <param name="logger">日志系统</param>
        [DebuggerHidden, DebuggerStepThrough] public static void SetLogger(ILogger logger) => s_Logger = logger ?? Logger.Instance;

        /// <summary>
        ///   <para>启用/禁用日志系统</para>
        /// </summary>
        /// <param name="enabled">是否启用</param>
        [DebuggerHidden, DebuggerStepThrough] public static void EnableLog(bool enabled) => s_Logger.IsEnabled = enabled;
        
        /// <summary>
        ///   <para>输出日志</para>
        /// </summary>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] public static void Log(object msg) => s_Logger.Log(msg);
        
        /// <summary>
        ///   <para>输出日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough] public static void Log(string format, params object[] args) => s_Logger.Log(format, args);
        
        /// <summary>
        ///   <para>输出警告日志</para>
        /// </summary>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] public static void LogWarning(object msg) => s_Logger.LogWarning(msg);
        
        /// <summary>
        ///   <para>输出警告日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough] public static void LogWarning(string format, params object[] args) => s_Logger.LogWarning(format, args);
        
        /// <summary>
        ///   <para>输出错误日志</para>
        /// </summary>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] public static void LogError(object msg) => s_Logger.LogError(msg);
        
        /// <summary>
        ///   <para>输出错误日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough] public static void LogError(string format, params object[] args) => s_Logger.LogError(format, args);
        
        /// <summary>
        ///   <para>输出异常日志</para>
        /// </summary>
        /// <param name="exception">异常</param>
        [DebuggerHidden, DebuggerStepThrough] public static void LogException(Exception exception) => s_Logger.LogException(exception);
        
        /// <summary>
        ///   <para>断言</para>
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] public static void Assert(bool condition, object msg) => s_Logger.Assert(condition, msg);
    }
}