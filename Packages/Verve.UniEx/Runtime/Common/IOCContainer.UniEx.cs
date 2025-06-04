#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using System;
    using UnityEngine;

    
    public partial class IOCContainer : Verve.IOCContainer
    {
        public override object ResolveOrRegister(System.Type type, params object[] args)
        {
            return ResolveOrRegister(type, _ =>
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    return GameObject.FindObjectOfType(type);
                }
                else
                {
                    return Activator.CreateInstance(type, args);
                }
            });
        }

        public override T ResolveOrRegister<T>(params object[] args)
        {
            return ResolveOrRegister<T>(_ =>
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
                {
                    return GameObject.FindObjectOfType(typeof(T));
                }
                else
                {
                    return Activator.CreateInstance(typeof(T), args);
                }
            });
        }
    }
}

#endif