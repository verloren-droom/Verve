#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.File
{
    using Verve;
    using UnityEngine;
    using System.Collections.Generic;


    /// <summary>
    /// 文件功能数据
    /// </summary>
    public partial class FileFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.File";

        public override IReadOnlyCollection<string> Dependencies => new string[] { "VerveUniEx.Serializable", "VerveUniEx.Platform" };

        public override string FeatureName => m_FeatureName;
        

        public override IGameFeature CreateFeature()
        {
            return new FileFeature();
        }
    }
}

#endif