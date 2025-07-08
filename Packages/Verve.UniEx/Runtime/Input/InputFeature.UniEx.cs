#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Input
{
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif
    
    
    /// <summary>
    /// 输入功能
    /// </summary>
    [System.Serializable]
    public partial class InputFeature : Verve.Input.InputFeature
    {
        protected readonly PlayerInput m_Input;
        
        
        public InputFeature(PlayerInput input = null)
        {
            m_Input = input;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
#if ENABLE_LEGACY_INPUT_MANAGER
            RegisterSubmodule(new InputManagerSubmodule());
#endif
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
            RegisterSubmodule(new InputSystemSubmodule(m_Input));
#endif
        }
    }
}

#endif