namespace Verve
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
#if UNITY_5_3_OR_NEWER
    using LogType = UnityEngine.LogType;
#endif
    
#if !UNITY_5_3_OR_NEWER
    /// <summary>
    ///   <para>日志类型。</para>
    /// </summary>
    internal enum LogType : byte
    {
        /// <summary>
        ///   <para>日志。</para>
        /// </summary>
        Log = 0,
        /// <summary>
        ///   <para>警告。</para>
        /// </summary>
        Warning = 1,
        /// <summary>
        ///   <para>错误。</para>
        /// </summary>
        Error = 2,
        /// <summary>
        ///   <para>异常。</para>
        /// </summary>
        Exception = 3,
        /// <summary>
        ///   <para>断言。</para>
        /// </summary>
        Assert = 4,
    }
#endif
    
    
    /// <summary>
    ///   <para>游戏入口；日志部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>日志器；借用当前应用的实现。</para>
        /// </summary>
        private static ILogger LogWriter => GetTool<ILogger>();
        /// <summary>
        ///   <para>日志输出开关；不改变日志工具实例。</para>
        /// </summary>
        private static volatile bool s_LogEnabled = true;

#if DEBUG
        /// <summary>
        ///   <para>日志记录上限。</para>
        /// </summary>
        private const int MaxLogRecords = 2000;
        /// <summary>
        ///   <para>日志锁。</para>
        /// </summary>
        private static readonly object s_LogLock = new object();
        /// <summary>
        ///   <para>日志记录。</para>
        /// </summary>
        private static LogRecord[] s_LogRecords = new LogRecord[MaxLogRecords];
        /// <summary>
        ///   <para>日志数量。</para>
        /// </summary>
        private static int s_LogCount;
        /// <summary>
        ///   <para>下次写入日志的索引。</para>
        /// </summary>
        private static int s_LogNextIndex;
        /// <summary>
        ///   <para>日志序号。</para>
        /// </summary>
        private static long s_LogSequence;

        /// <summary>
        ///   <para>日志记录。</para>
        /// </summary>
        internal readonly struct LogRecord
        {
            /// <summary>
            ///   <para>序号。</para>
            /// </summary>
            public readonly long sequence;
            /// <summary>
            ///   <para>时间。</para>
            /// </summary>
            public readonly DateTime time;
            /// <summary>
            ///   <para>帧号。</para>
            /// </summary>
            public readonly int frame;
            /// <summary>
            ///   <para>线程标识。</para>
            /// </summary>
            public readonly int threadId;
            /// <summary>
            ///   <para>类型。</para>
            /// </summary>
            public readonly LogType type;
            /// <summary>
            ///   <para>调用方成员。</para>
            /// </summary>
            public readonly string callerMember;
            /// <summary>
            ///   <para>调用方文件。</para>
            /// </summary>
            public readonly string callerFile;
            /// <summary>
            ///   <para>调用方行号。</para>
            /// </summary>
            public readonly int callerLine;
            /// <summary>
            ///   <para>消息。</para>
            /// </summary>
            public readonly string message;
            /// <summary>
            ///   <para>栈跟踪。</para>
            /// </summary>
            public readonly string stackTrace;

            /// <summary>
            ///   <para>创建日志记录。</para>
            /// </summary>
            /// <param name="sequence">序号。</param>
            /// <param name="time">时间。</param>
            /// <param name="frame">帧号。</param>
            /// <param name="threadId">线程标识。</param>
            /// <param name="type">日志类型。</param>
            /// <param name="callerMember">调用方成员。</param>
            /// <param name="callerFile">调用方文件。</param>
            /// <param name="callerLine">调用方行。</param>
            /// <param name="message">消息内容。</param>
            /// <param name="stackTrace">栈跟踪。</param>
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

        /// <summary>
        ///   <para>记录日志。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <param name="message">消息内容。</param>
        /// <param name="stackTrace">栈跟踪。</param>
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
                    IsMainThread ? UnityEngine.Time.frameCount : -1,
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

        /// <summary>
        ///   <para>获取调用方信息。</para>
        /// </summary>
        private static (string member, string file, int line) GetCallerInfo()
        {
            var frame = new StackTrace(3, true).GetFrame(0);
            if (frame == null)
            {
                return (null, null, 0);
            }

            var method = frame.GetMethod();
            if (method == null)
            {
                return (null, null, 0);
            }

            var declaringType = method.DeclaringType;
            var member = declaringType == null ? method.Name : $"{declaringType.FullName}.{method.Name}";
            return (member, frame.GetFileName(), frame.GetFileLineNumber());
        }

        /// <summary>
        ///   <para>调试获取日志记录。</para>
        /// </summary>
        /// <param name="output">接收结果的集合。</param>
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

        /// <summary>
        ///   <para>调试清空日志。</para>
        /// </summary>
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
        ///   <para>启用/禁用日志系统。</para>
        /// </summary>
        /// <param name="enabled">是否启用。</param>
        [DebuggerHidden, DebuggerStepThrough] public static void EnableLog(bool enabled) => s_LogEnabled = enabled;
        
        /// <summary>
        ///   <para>输出日志。</para>
        /// </summary>
        /// <param name="msg">日志内容。</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        // Console菜单需要勾选Strip logging callstack
        [UnityEngine.HideInCallstack]
#endif
        public static void Log(object msg)
        {
            if (!s_LogEnabled) return;
#if DEBUG
            if (msg != null) RecordLog(LogType.Log, msg.ToString(), null);
#endif
            LogWriter.Log(msg);
        }
        
        /// <summary>
        ///   <para>输出日志。</para>
        /// </summary>
        /// <param name="format">日志格式化内容。</param>
        /// <param name="args">日志格式化参数。</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void Log(string format, params object[] args)
        {
            if (!s_LogEnabled) return;
            var text = string.Format(format, args);
#if DEBUG
            if (!string.IsNullOrEmpty(text)) RecordLog(LogType.Log, text, null);
#endif
            LogWriter.Log((object)text);
        }
        
        /// <summary>
        ///   <para>输出警告日志。</para>
        /// </summary>
        /// <param name="msg">日志内容。</param>
        [DebuggerHidden, DebuggerStepThrough] 
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogWarning(object msg)
        {
            if (!s_LogEnabled) return;
#if DEBUG
            if (msg != null) RecordLog(LogType.Warning, msg.ToString(), null);
#endif
            LogWriter.LogWarning(msg);
        }
        
        /// <summary>
        ///   <para>输出警告日志。</para>
        /// </summary>
        /// <param name="format">日志格式化内容。</param>
        /// <param name="args">日志格式化参数。</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogWarning(string format, params object[] args)
        {
            if (!s_LogEnabled) return;
            var text = string.Format(format, args);
#if DEBUG
            if (!string.IsNullOrEmpty(text)) RecordLog(LogType.Warning, text, null);
#endif
            LogWriter.LogWarning((object)text);
        }
        
        /// <summary>
        ///   <para>输出错误日志。</para>
        /// </summary>
        /// <param name="msg">日志内容。</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogError(object msg)
        {
            if (!s_LogEnabled) return;
#if DEBUG
            if (msg != null) RecordLog(LogType.Error, msg.ToString(), null);
#endif
            LogWriter.LogError(msg);
        }
        
        /// <summary>
        ///   <para>输出错误日志。</para>
        /// </summary>
        /// <param name="format">日志格式化内容。</param>
        /// <param name="args">日志格式化参数。</param>
        [DebuggerHidden, DebuggerStepThrough] 
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogError(string format, params object[] args)
        {
            if (!s_LogEnabled) return;
            var text = string.Format(format, args);
#if DEBUG
            if (!string.IsNullOrEmpty(text)) RecordLog(LogType.Error, text, null);
#endif
            LogWriter.LogError((object)text);
        }
        
        /// <summary>
        ///   <para>输出异常日志。</para>
        /// </summary>
        /// <param name="exception">异常。</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void LogException(Exception exception)
        {
            if (!s_LogEnabled) return;
#if DEBUG
            if (exception != null) RecordLog(LogType.Exception, exception.ToString(), exception.StackTrace);
#endif
            LogWriter.LogException(exception);
        }
        
        /// <summary>
        ///   <para>断言。</para>
        /// </summary>
        /// <param name="condition">条件。</param>
        /// <param name="msg">日志内容。</param>
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public static void Assert(bool condition, object msg)
        {
            if (!s_LogEnabled) return;
#if DEBUG
            if (!condition && msg != null) RecordLog(LogType.Assert, msg.ToString(), null);
#endif
            LogWriter.Assert(condition, msg);
        }
    }
}