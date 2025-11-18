#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace Verve.UniEx.Samples
{
    using MVC;
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>虚拟摇杆活动</para>
    /// </summary>
    [AddComponentMenu("Verve/Samples/VirtualJoystickActivity")]
    public class VirtualJoystickActivity : ActivityBase
    {
        protected override void OnInitialized()
        {
            AddModel<VirtualJoystickModel>();
        }
    }
}

#endif