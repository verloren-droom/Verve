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

        /// <summary> 功能名称（默认为当前程序集名称） </summary>
        public virtual string FeatureName => GetType().Assembly.GetName().Name;
        public virtual IReadOnlyCollection<string> Dependencies => Array.Empty<string>();
        public bool AutoLoad => m_AutoLoad;
        public bool AutoActivate => m_AutoActivate;
        public bool IsPersistent => m_IsPersistent;
        public string Description => m_Description;


        /// <summary> 创建功能实例 </summary>
        public abstract IGameFeature CreateFeature();
    }
}

#endif