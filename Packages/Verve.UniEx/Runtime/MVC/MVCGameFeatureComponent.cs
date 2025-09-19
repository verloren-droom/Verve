#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.MVC
{
    using System;
    using UnityEngine;
    
    
    [Serializable, GameFeatureComponentMenu("Verve/MVC")]
    public sealed class MVCGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("视图根节点")] private GameFeatureParameter<GameObject> m_ViewRoot = new GameFeatureParameter<GameObject>();
        public Transform ViewRoot => m_ViewRoot.Value.transform;
    }
}

#endif