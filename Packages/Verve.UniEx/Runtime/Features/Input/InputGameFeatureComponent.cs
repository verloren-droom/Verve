#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Input
{
    using UnityEngine;
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif
    
    
    /// <summary>
    /// 输入游戏功能组件
    /// </summary>
    [System.Serializable, GameFeatureComponentMenu("Verve/Input")]
    public sealed class InputGameFeatureComponent : GameFeatureComponent
    {

    }
}

#endif