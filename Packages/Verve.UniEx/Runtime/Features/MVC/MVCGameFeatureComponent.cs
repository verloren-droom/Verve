#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.MVC
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>MVC游戏功能组件</para>
    /// </summary>
    [Serializable, GameFeatureComponentMenu("Verve/MVC")]
    public sealed class MVCGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("视图根节点")] private GameFeatureParameter<GameObject> m_ViewRoot = new GameFeatureParameter<GameObject>();
        
        
        /// <summary>
        ///   <para>视图根节点</para>
        /// </summary>
        public Transform ViewRoot => m_ViewRoot.Value.transform;
    }
}

#endif