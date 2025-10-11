#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Debug
{
    using System;
    using UnityEngine;

    
    /// <summary>
    /// 调试游戏功能组件
    /// </summary>
    [Serializable, GameFeatureComponentMenu("Verve/Debug")]
    public sealed class DebuggerGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("显示/隐藏调试控制台快捷键")] private GameFeatureParameter<KeyCode> m_CommandToggleKey = new GameFeatureParameter<KeyCode>(KeyCode.F10);


        /// <summary> 显示/隐藏调试控制台快捷键 </summary>
        public KeyCode CommandToggleKey => m_CommandToggleKey.Value;
    }
}

#endif