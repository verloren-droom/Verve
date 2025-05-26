#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Input
{
    using Verve.Unit;
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif
    
    
    [CustomUnit("Input"), System.Serializable]
    public partial class InputUnit : Verve.Input.InputUnit
    {
        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
#if ENABLE_LEGACY_INPUT_MANAGER
            AddService(new InputManagerService());
#endif
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
            AddService(new InputSystemService(args.Length > 0 ? args[0] as PlayerInput : null));
#endif
        }
    }
}
    
#endif