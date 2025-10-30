#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace Verve.UniEx.Sample
{
    using MVC;
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>虚拟摇杆活动</para>
    /// </summary>
    [AddComponentMenu("Verve/Sample/VirtualJoystickActivity")]
    public class VirtualJoystickActivity : ActivityBase
    {
        protected override void OnInitialized()
        {
            AddModel<VirtualJoystickModel>();
        }
    }
}

#endif