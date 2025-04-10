namespace Verve.Input
{
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using UnityEngine;
    using UnityEngine.InputSystem;
    
    
    /// <summary>
    /// 新版输入系统（Input System）
    /// </summary>
    public sealed partial class InputSystemHandle : IInputHandle
    {

    }
#endif
}