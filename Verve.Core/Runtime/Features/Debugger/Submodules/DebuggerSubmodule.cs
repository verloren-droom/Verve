#if UNITY_5_3_OR_NEWER

namespace Verve.Debug
{
    using System;

    
    /// <summary>
    ///   <para>调试器子模块基类</para>
    /// </summary>
    [Serializable, SkipInStackTrace]
    public abstract class DebuggerSubmodule : TickableGameFeatureSubmodule<DebuggerGameFeatureComponent>
    {
        
    }
}

#endif