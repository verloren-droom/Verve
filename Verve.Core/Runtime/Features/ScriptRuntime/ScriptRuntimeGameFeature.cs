#if UNITY_5_3_OR_NEWER

namespace Verve.ScriptRuntime
{
    /// <summary>
    ///   <para>脚本运行时模块</para>
    ///   <para>负责脚本的动态加载、执行和管理</para>
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/ScriptRuntime", Description = "脚本运行时模块 - 负责脚本的动态加载、执行和管理")]
    public sealed class ScriptRuntimeGameFeature : GameFeatureModule
    {
        
    }
}

#endif