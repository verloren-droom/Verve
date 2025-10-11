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
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
        [SerializeField, Tooltip("玩家输入")] private GameFeatureParameter<PlayerInput> m_Input = new GameFeatureParameter<PlayerInput>();
        
        /// <summary> 玩家输入 </summary>
        public PlayerInput Input => m_Input.Value;
#endif
    }
}

#endif