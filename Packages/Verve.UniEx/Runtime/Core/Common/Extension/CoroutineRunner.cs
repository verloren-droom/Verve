#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>协程运行器</para>
    /// </summary>
    internal class CoroutineRunner : ComponentInstanceBase<CoroutineRunner>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}

#endif