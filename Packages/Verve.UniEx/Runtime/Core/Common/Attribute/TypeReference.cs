#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>类型引用</para>
    ///   <para>用于序列化类型</para>
    /// </summary>
    [Serializable]
    public class TypeReference<T> : ISerializationCallbackReceiver where T : class
    {
        [SerializeField, ReadOnly, HideInInspector] private string m_TypeName = typeof(T).AssemblyQualifiedName;
#if UNITY_2019_3_OR_NEWER
        [SerializeReference]
#endif
        [SerializeField] private T m_Value;
        private Type m_Type;

        public Type Type
        {
            get
            {
                if (m_Type == null && !string.IsNullOrEmpty(m_TypeName))
                {
                    m_Type = Type.GetType(m_TypeName);
                }
                return m_Type ?? typeof(T);
            }
        }

        public T Value
        {
            get => m_Value;
            set => m_Value = value;
        }
        

        public void OnBeforeSerialize()
        {
            if (m_Type != null)
            {
                m_TypeName = m_Type.AssemblyQualifiedName;
            }
        }
    
        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_TypeName))
            {
                m_Type = Type.GetType(m_TypeName);
                if (m_Type == null || !typeof(T).IsAssignableFrom(m_Type))
                {
                    m_Type = typeof(T);
                    m_TypeName = m_Type.AssemblyQualifiedName;
                }
            }
            else
            {
                m_Type = typeof(T);
            }
        }
    
#if UNITY_EDITOR
        private static Dictionary<Type, Type[]> s_assignableTypesCache = new Dictionary<Type, Type[]>();
        private static DateTime s_lastCacheClearTime = DateTime.Now;

        public Type[] GetAssignableTypes()
        {
            if ((DateTime.Now - s_lastCacheClearTime).TotalMinutes > 5)
            {
                s_assignableTypesCache.Clear();
                s_lastCacheClearTime = DateTime.Now;
            }
    
            Type baseType = typeof(T);
    
            if (s_assignableTypesCache.TryGetValue(baseType, out Type[] cachedTypes))
            {
                return cachedTypes;
            }
    
            Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic) 
                .SelectMany(assembly => 
                {
                    try 
                    {
                        return assembly.GetTypes();
                    }
                    catch (System.Reflection.ReflectionTypeLoadException e)
                    {
                        Debug.LogWarning($"Failed to load types from assembly: {assembly.FullName}. " +
                                         $"Loader exceptions: {string.Join(", ", e.LoaderExceptions.Select(ex => ex.Message))}");
                        return e.Types.Where(t => t != null);
                    }
                })
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType)                
                .ToArray();
    
            s_assignableTypesCache[baseType] = types;
            return types;
        }
#endif
    }
}

#endif