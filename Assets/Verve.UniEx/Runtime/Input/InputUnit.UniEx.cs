namespace VerveUniEx.Input
{
    
#if UNITY_5_3_OR_NEWER
    using Verve.Unit;
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif
    
    
    [CustomUnit("Input")]
    public partial class InputUnit : Verve.Input.InputUnit
    {
        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
#if ENABLE_LEGACY_INPUT_MANAGER
            Register(() => new InputManagerService());
#endif
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
            Register(() => new InputSystemService(args.Length > 0 ? args[0] as PlayerInput : null));
#endif
        }
    }
#endif
    
}