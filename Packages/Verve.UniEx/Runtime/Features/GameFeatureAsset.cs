#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using Verve;
    using System;
    using UnityEngine;
    using System.Collections.Generic;


    /// <summary>
    /// 功能资源基类
    /// </summary>
    public abstract partial class GameFeatureAsset : ScriptableObject, IGameFeatureData
    {
        [SerializeField, Tooltip("功能描述"), TextArea] private string m_Description;
        [SerializeField, Tooltip("是否在注册后自动加载")] private bool m_AutoLoad = true;
        [SerializeField, Tooltip("是否在加载后自动激活")] private bool m_AutoActivate = false;
        [SerializeField, Tooltip("是否常驻功能")] private bool m_IsPersistent = true;

        public abstract string FeatureName { get; }
        public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();
        public bool AutoLoad => m_AutoLoad;
        public bool AutoActivate => m_AutoActivate;
        public bool IsPersistent => m_IsPersistent;


        /// <summary> 创建功能实例 </summary>
        public abstract IGameFeature CreateFeature();
    }
}

#endif