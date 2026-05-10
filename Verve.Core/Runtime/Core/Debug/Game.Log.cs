namespace Verve
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
#if UNITY_5_3_OR_NEWER
    using LogType = UnityEngine.LogType;
#endif
    
    
#if !UNITY_5_3_OR_NEWER
    internal enum LogType : byte
    {
        Log = 0,
        Warning = 1,
        Error = 2,
        Exception = 3,
        Assert = 4,
    }
#endif
    
    
    /// <summary>
    ///   <para>游戏入口：日志部分</para>
    /// </summary>
    public static partial class Game
    {
        private static ILogger s_Logger = Logger.Instance;

#if DEBUG
        private const int MaxLogRecords = 2000;
        private static readonly object s_LogLock = new object();
        private static LogRecord[] s_LogRecords = new LogRecord[MaxLogRecords];
        private static int s_LogCount;
        private static int s_LogNextIndex;
        private static long s_LogSequence;

        internal readonly struct LogRecord
        {
            public readonly long sequence;
            public readonly DateTime time;
            public readonly int frame;
            public readonly int threadId;
            public readonly LogType type;
            public readonly string callerMember;
            public readonly string callerFile;
            public readonly int callerLine;
            public readonly string message;
            public readonly string stackTrace;

            public LogRecord(
                long sequence,
                DateTime time,
                int frame,
                int threadId,
                LogType type,
                string callerMember,
                string callerFile,
                int callerLine,
                string message,
                string stackTrace)
            {
                this.sequence = sequence;
                this.time = time;
                this.frame = frame;
                this.threadId = threadId;
                this.type = type;
                this.callerMember = callerMember;
                this.callerFile = callerFile;
                this.callerLine = callerLine;
                this.message = message;
                this.stackTrace = stackTrace;
            }
        }

        private static void RecordLog(LogType type, string message, string stackTrace)
        {
            if (string.IsNullOrEmpty(message)) return;
            var (callerMember, callerFile, callerLine) = GetCallerInfo();
            lock (s_LogLock)
            {
                var index = s_LogNextIndex++;
                if (s_LogNextIndex >= s_LogRecords.Length) s_LogNextIndex = 0;
                s_LogRecords[index] = new LogRecord(
                    ++s_LogSequence,
                    DateTime.Now,
#if UNITY_5_3_OR_NEWER
                    UnityEngine.Time.frameCount,
#else
                    -1,
#endif
                    Environment.CurrentManagedThreadId,
                    type,
                    callerMember,
                    callerFile,
                    callerLine,
                    message,
                    stackTrace);
                if (s_LogCount < s_LogRecords.Length) s_LogCount++;
            }
        }

        private static (string member, string file, int line) GetCallerInfo()
        {
            string member = null;
            string file = null;
            int line = 0;
            try
            {
                var frame = new StackTrace(3, true).GetFrame(0);
                if (frame == null) return (null, null, 0);

                var method = frame.GetMethod();
                if (method == null) return (null, null, 0);

                var declaringType = method.DeclaringType;
                member = declaringType == null ? method.Name : $"{declaringType.FullName}.{method.Name}";
                file = frame.GetFileName();
                line = frame.GetFileLineNumber();
            }
            catch { }
            
            return (member, file, line);
        }

        internal static void DebugGetLogRecords(List<LogRecord> output)
        {
            if (output == null) return;
            output.Clear();
            lock (s_LogLock)
            {
                if (s_LogCount == 0) return;
                var start = s_LogCount == s_LogRecords.Length ? s_LogNextIndex : 0;
                for (int i = 0; i < s_LogCount; i++)
                {
                    var idx = start + i;
                    if (idx >= s_LogRecords.Length) idx -= s_LogRecords.Length;
                    output.Add(s_LogRecords[idx]);
                }
            }
        }

        internal static void DebugClearLogs()
        {
            lock (s_LogLock)
            {
                s_LogCount = 0;
                s_LogNextIndex = 0;
                s_LogSequence = 0;
                Array.Clear(s_LogRecords, 0, s_LogRecords.Length);
            }
        }
#endif
        
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
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        // Console菜单需要勾选Strip logging callstack
        [UnityEngine.HideInCallstack]
#endif
        public static void Log(object msg)
        {
#if DEBUG
            if (msg != null) RecordLog(LogType.Log, msg.ToString(), null);
#endif
            s_Logger.Log(msg);
        }
        
        /// <summary>
        ///   <para>输出日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void Log(string format, params object[] args)
        {
            var text = SafeFormat(format, args);
#if DEBUG
            if (!string.IsNullOrEmpty(text)) RecordLog(LogType.Log, text, null);
#endif
            s_Logger.Log(format, args);
        }
        
        /// <summary>
        ///   <para>输出警告日志</para>
        /// </summary>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough] 
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogWarning(object msg)
        {
#if DEBUG
            if (msg != null) RecordLog(LogType.Warning, msg.ToString(), null);
#endif
            s_Logger.LogWarning(msg);
        }
        
        /// <summary>
        ///   <para>输出警告日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogWarning(string format, params object[] args)
        {
            var text = SafeFormat(format, args);
#if DEBUG
            if (!string.IsNullOrEmpty(text)) RecordLog(LogType.Warning, text, null);
#endif
            s_Logger.LogWarning(format, args);
        }
        
        /// <summary>
        ///   <para>输出错误日志</para>
        /// </summary>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogError(object msg)
        {
#if DEBUG
            if (msg != null) RecordLog(LogType.Error, msg.ToString(), null);
#endif
            s_Logger.LogError(msg);
        }
        
        /// <summary>
        ///   <para>输出错误日志</para>
        /// </summary>
        /// <param name="format">日志格式化内容</param>
        /// <param name="args">日志格式化参数</param>
        [DebuggerHidden, DebuggerStepThrough] 
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogError(string format, params object[] args)
        {
            var text = SafeFormat(format, args);
#if DEBUG
            if (!string.IsNullOrEmpty(text)) RecordLog(LogType.Error, text, null);
#endif
            s_Logger.LogError(format, args);
        }
        
        /// <summary>
        ///   <para>输出异常日志</para>
        /// </summary>
        /// <param name="exception">异常</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogException(Exception exception)
        {
#if DEBUG
            if (exception != null) RecordLog(LogType.Exception, exception.ToString(), exception.StackTrace);
#endif
            s_Logger.LogException(exception);
        }
        
        /// <summary>
        ///   <para>断言</para>
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="msg">日志内容</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void Assert(bool condition, object msg)
        {
#if DEBUG
            if (!condition && msg != null) RecordLog(LogType.Assert, msg.ToString(), null);
#endif
            s_Logger.Assert(condition, msg);
        }
        
        private static string SafeFormat(string format, object[] args)
        {
            if (string.IsNullOrEmpty(format)) return null;
            if (args == null || args.Length == 0) return format;
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }
    }
}