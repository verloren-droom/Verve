#if UNITY_5_3_OR_NEWER

namespace Verve.ScriptRuntime
{
    using System;
    using UnityEngine;

    
    /// <summary>
    ///   <para>脚本运行时功能组件</para>
    /// </summary>
    [Serializable, GameFeatureComponentMenu("Verve/ScriptRuntime")]
    public sealed class ScriptRuntimeGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("AOT程序集目录")] private PathParameter m_AOTAssemblyFolder
            = new PathParameter("");
        
        [SerializeField, Tooltip("AOT程序集补充元数据列表")] private GameFeatureParameter<string[]> m_PatchedAOTAssemblyNames
            = new GameFeatureParameter<string[]>(new []
        {
            "System",
            "System.Core",
            "mscorlib",
            
            // 框架内置的AOT程序集
            "Verve.Core",
            "Verve.UniEx",
            "Verve.UniEx.ScriptRuntime",
            "Verve.Loader",
            "Verve.UniEx.Loader",
        });

        [SerializeField, Tooltip("程序集对应的热更资源配置路径")] private GameFeatureParameter<AssemblyFeaturePath[]> m_AssemblyFeatureMappings
            = new GameFeatureParameter<AssemblyFeaturePath[]>(Array.Empty<AssemblyFeaturePath>());
        
        /// <summary>
        ///   <para>AOT程序集目录</para>
        /// </summary>
        public string AOTAssemblyFolder => m_AOTAssemblyFolder.Value;
        
        /// <summary>
        ///   <para>AOT程序集补充元数据列表</para>
        /// </summary>
        public string[] PatchedAOTAssemblyNames => m_PatchedAOTAssemblyNames.Value;
        
        /// <summary>
        ///   <para>程序集对应的热更资源配置路径</para>
        /// </summary>
        public AssemblyFeaturePath[] AssemblyFeatureMappings => m_AssemblyFeatureMappings.Value;
    }
    
    
    /// <summary>
    ///   <para>程序集对应的热更资源配置</para>
    /// </summary>
    [Serializable]
    public class AssemblyFeaturePath
    {
        [Tooltip("程序集名称")] public string assemblyName;
        [Tooltip("热更模块配置文件路径")] public string[] modulePaths;
        [Tooltip("热更组件配置文件路径")] public string[] componentPaths;
    }
}

#endif