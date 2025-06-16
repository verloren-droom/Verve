#if UNITY_EDITOR

namespace VerveUniEx.MVC
{
    using UnityEngine;
    
    
    [DisallowMultipleComponent]
    public abstract partial class ViewBase
    {
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(m_ViewName))
            {
                m_ViewName = gameObject.name;
            }
        }
    }
}

#endif