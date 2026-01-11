namespace Verve.Samples
{
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>虚拟摇杆活动</para>
    /// </summary>
    [AddComponentMenu("Verve/Samples/VirtualJoystickActivity")]
    public sealed class VirtualJoystickActivity : ActivityBase
    {
        protected override void OnInitialized()
        {
            AddModel<VirtualJoystickModel>();
        }
    }
}