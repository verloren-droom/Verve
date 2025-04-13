namespace Verve.Input
{
    
#if UNITY_5_3_OR_NEWER && ENABLE_LEGACY_INPUT_MANAGER
    using System;
    using UnityEngine;


    /// <summary>
    /// 旧版输入系统（Input Manager） 
    /// </summary>
    public sealed partial class InputManagerService : InputServiceBase
    {
        public bool IsValid => true;
        
        public override float GetAxis(string axisName) => IsEnabled ? Input.GetAxis(axisName) : 0.0f;
        public override Vector2 GetMousePosition() => IsEnabled ? Input.mousePosition : Vector2.zero;
        public override bool GetButtonDown(string buttonName) => IsEnabled && Input.GetButtonDown(buttonName);
        public override bool GetButtonUp(string buttonName) => IsEnabled && Input.GetButtonUp(buttonName);
        public override bool GetButton(string buttonName) => IsEnabled && Input.GetButton(buttonName);
    }
#endif
    
}