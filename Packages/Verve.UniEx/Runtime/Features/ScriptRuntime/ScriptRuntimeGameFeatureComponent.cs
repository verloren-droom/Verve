using UnityEngine.Serialization;

#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.ScriptRuntime
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;

    
    [Serializable, GameFeatureComponentMenu("Verve/ScriptRuntime")]
    public sealed class ScriptRuntimeGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("AOT程序集目录")] private PathParameter m_AOTAssemblyFolder
            = new PathParameter("");
        /// <summary>
        /// AOT程序集目录
        /// </summary>
        public string AOTAssemblyFolder => m_AOTAssemblyFolder.Value;
        
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
        /// <summary>
        /// AOT程序集补充元数据列表
        /// </summary>
        public string[] PatchedAOTAssemblyNames => m_PatchedAOTAssemblyNames.Value;

        [SerializeField, Tooltip("程序集对应的热更资源配置路径")] private GameFeatureParameter<AssemblyFeaturePath[]> m_AssemblyFeatureMappings
            = new GameFeatureParameter<AssemblyFeaturePath[]>(Array.Empty<AssemblyFeaturePath>());
        /// <summary>
        /// 程序集对应的热更资源配置路径
        /// </summary>
        public AssemblyFeaturePath[] AssemblyFeatureMappings => m_AssemblyFeatureMappings.Value;
    }
    
    
    [Serializable]
    public class AssemblyFeaturePath
    {
        [Tooltip("程序集名称")] public string assemblyName;
        [Tooltip("热更模块配置文件路径")] public string[] modulePaths;
        [Tooltip("热更组件配置文件路径")] public string[] componentPaths;
    }
}

#endif