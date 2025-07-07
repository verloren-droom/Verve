#if UNITY_EDITOR

namespace VerveEditor.UniEx.Debugger
{
    using VerveUniEx.Debugger;
    
    
    public static class DebuggerUnitExtension
    {
        public static string GetMemory(this DebuggerFeature debugger)
        {
#if UNITY_5_6_OR_NEWER
            return (UnityEngine.Profiling.Profiler.usedHeapSizeLong / 1024).ToString() + " kb";
#elif UNITY_5_5_OR_NEWER
                return (UnityEngine.Profiling.Profiler.usedHeapSize / 1024).ToString() + " kb";
#else
                return (Profiler.usedHeapSize / 1024).ToString() + " kb";
#endif
        }
        
    }
}

#endif