#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.HotFix
{
    using System;
    using UnityEngine;

    
    [Serializable, GameFeatureComponentMenu("Verve/HotFix")]
    public sealed class HotFixGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("缓存目录")] private GameFeatureParameter<string> m_CachePath = new GameFeatureParameter<string>("");
        /// <summary>
        /// 缓存目录
        /// </summary>
        public string CachePath => m_CachePath.Value;
        [SerializeField, Tooltip("AOT程序集补充元数据列表")] private GameFeatureParameter<string[]> m_PatchedAOTAssemblyNames = new GameFeatureParameter<string[]>(Array.Empty<string>());
        /// <summary>
        /// AOT程序集补充元数据列表
        /// </summary>
        public string[] PatchedAOTAssemblyNames => m_PatchedAOTAssemblyNames.Value;
    }
}

#endif