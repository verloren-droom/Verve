#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>游戏功能组件基类</para>
    ///   <para>用于管理游戏功能参数，仅用于参数声明供功能子模块使用，不应该在类中执行逻辑代码</para>
    /// </summary>
    [Serializable]
    public abstract class GameFeatureComponent : ScriptableObject, IGameFeatureComponent
    {
        [SerializeReference, HideInInspector] private List<GameFeatureParameter> m_Parameters = new List<GameFeatureParameter>();

        public IReadOnlyCollection<IGameFeatureParameter> Parameters => m_Parameters.AsReadOnly();
        

        protected virtual void OnEnable()
        {
            // this.FindParameters(m_Parameters);
            if (m_Parameters == null)
                return;
            for (int i = 0; i < m_Parameters.Count; i++)
            {
                m_Parameters[i]?.OnEnable();
            }
        }
        
        protected virtual void OnDisable()
        {
            if (m_Parameters == null)
                return;

            for (int i = 0; i < m_Parameters.Count; i++)
            {
                m_Parameters[i]?.OnDisable();
            }
        }
        
        /// <summary>
        ///   <para>释放参数资源</para>
        /// </summary>
        public void Release()
        {
            if (m_Parameters == null)
                return;

            for (int i = 0; i < m_Parameters.Count; i++)
            {
                m_Parameters[i]?.Release();
            }
        }
    }
}

#endif