#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Event
{
    using UnityEngine;
    
    
    public class PropertyProxy<T> : Verve.Event.PropertyProxy<T>
    {
        [SerializeField]
        protected new T m_Value; 
    }
}

#endif