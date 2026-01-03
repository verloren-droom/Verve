#if UNITY_5_3_OR_NEWER

namespace Verve.Debug
{
    using System;
    using UnityEngine;

    
    /// <summary>
    ///   <para>调试游戏功能组件</para>
    /// </summary>
    [Serializable, GameFeatureComponentMenu("Verve/Debug")]
    public sealed class DebuggerGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("显示/隐藏调试控制台快捷键")] private GameFeatureParameter<KeyCode> m_CommandToggleKey = new GameFeatureParameter<KeyCode>(KeyCode.F10);


        /// <summary>
        ///   <para>显示/隐藏调试控制台快捷键</para>
        /// </summary>
        public KeyCode CommandToggleKey => m_CommandToggleKey.Value;
    }
}

#endif