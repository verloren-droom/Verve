#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Debug
{
    using System;
    using UnityEngine;

    
    [Serializable, GameFeatureComponentMenu("Verve/Debug")]
    public sealed class DebuggerGameFeatureComponent : GameFeatureComponent
    {
        [Tooltip("显示/隐藏调试控制台快捷键")] public GameFeatureParameter<KeyCode> commandToggleKey = new GameFeatureParameter<KeyCode>(KeyCode.F10);
    }
}

#endif