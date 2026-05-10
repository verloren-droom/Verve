namespace Verve
{
    using System;
    using System.Diagnostics;
    
    
    /// <summary>
    ///   <para>日志器</para>
    /// </summary>
    internal sealed class Logger : InstanceBase<Logger>, ILogger
    {
        public bool IsEnabled { get; set; } = true;

        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void Log(object msg)
        {
            if (msg == null || !IsEnabled) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(msg);
#else
            Console.WriteLine($"[LOG] {msg}");
#endif
        }

        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void Log(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format) || !IsEnabled) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogFormat(format, args);
#else
            Console.WriteLine($"[LOG] {FormatMessage(format, args)}");
#endif
        }

        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogWarning(object msg)
        {
            if (msg == null || !IsEnabled) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogWarning(msg);
#else
            Console.WriteLine($"[WARN] {msg}");
#endif
        }

        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogWarning(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format) || !IsEnabled) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogWarningFormat(format, args);
#else
            Console.WriteLine($"[WARN] {FormatMessage(format, args)}");
#endif
        }

        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogError(object msg)
        {
            if (msg == null || !IsEnabled) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogError(msg);
#else
            Console.WriteLine($"[ERROR] {msg}");
#endif
        }

        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogError(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format) || !IsEnabled) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogErrorFormat(format, args);
#else
            Console.WriteLine($"[ERROR] {FormatMessage(format, args)}");
#endif
        }

        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogException(Exception exception)
        {
            if (exception == null || !IsEnabled) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogException(exception);
#else
            Console.WriteLine($"[EXCEPTION] {exception}");
#endif
        }
        
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void Assert(bool condition, object msg)
        {
            if (msg == null || condition || !IsEnabled) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogAssertion(msg);
#else
            Console.WriteLine($"[ASSERT] {msg}");
#endif
        }

        private static string FormatMessage(string format, object[] args)
        {
            if (args == null || args.Length == 0) return format;
            return string.Format(format, args);
        }
    }
}