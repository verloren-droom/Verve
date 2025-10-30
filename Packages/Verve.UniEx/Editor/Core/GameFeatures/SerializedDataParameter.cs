#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    using UnityEditor;
    using System.Reflection;
    
    
    /// <summary>
    ///   <para>可序列化数据参数</para>
    /// </summary>
    public class SerializedDataParameter
    {
        public SerializedProperty property { get; private set; }
        public SerializedProperty value => property.FindPropertyRelative("m_Value");

        public SerializedDataParameter(SerializedProperty property)
        {
            this.property = property;
        }
        
        public object GetTargetObject()
        {
            return GetTargetObject(property);
        }
        
        /// <summary>
        ///   <para>获取目标对象</para>
        /// </summary>
        public T GetTargetObject<T>()
        {
            return (T)GetTargetObject(property);
        }

        private object GetTargetObject(SerializedProperty property)
        {

            object targetObject = property.serializedObject.targetObject;
            
            if (!string.IsNullOrEmpty(property.propertyPath))
            {
                targetObject = GetNestedObject(property.propertyPath, targetObject);
            }
            
            return targetObject;
        }
        
        private object GetNestedObject(string path, object obj)
        {
            string[] parts = path.Split('.');
            foreach (string part in parts)
            {
                if (obj == null) return null;
                
                Type type = obj.GetType();
                FieldInfo field = type.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    obj = field.GetValue(obj);
                }
                else
                {
                    PropertyInfo prop = type.GetProperty(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        obj = prop.GetValue(obj);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return obj;
        }
    }
}

#endif