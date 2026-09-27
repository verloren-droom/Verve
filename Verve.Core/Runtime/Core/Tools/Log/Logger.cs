namespace Verve
{
    using System;
    using System.Diagnostics;
    
    /// <summary>
    ///   <para>日志器。</para>
    /// </summary>
    internal sealed class Logger : ILogger
    {
        /// <inheritdoc />
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void Log(object msg)
        {
            if (msg == null) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(msg);
#else
            Console.WriteLine($"[LOG] {msg}");
#endif
        }

        /// <inheritdoc />
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void Log(string format, params object[] args) => Log((object)string.Format(format, args));

        /// <inheritdoc />
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogWarning(object msg)
        {
            if (msg == null) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogWarning(msg);
#else
            Console.WriteLine($"[WARN] {msg}");
#endif
        }

        /// <inheritdoc />
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogWarning(string format, params object[] args) => LogWarning((object)string.Format(format, args));

        /// <inheritdoc />
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogError(object msg)
        {
            if (msg == null) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogError(msg);
#else
            Console.WriteLine($"[ERROR] {msg}");
#endif
        }

        /// <inheritdoc />
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogError(string format, params object[] args) => LogError((object)string.Format(format, args));

        /// <inheritdoc />
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void LogException(Exception exception)
        {
            if (exception == null) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogException(exception);
#else
            Console.WriteLine($"[EXCEPTION] {exception}");
#endif
        }
        
        /// <inheritdoc />
        [DebuggerHidden, DebuggerStepThrough]
#if UNITY_2022_2_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void Assert(bool condition, object msg)
        {
            if (msg == null || condition) return;
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogAssertion(msg);
#else
            Console.WriteLine($"[ASSERT] {msg}");
#endif
        }
    }
}