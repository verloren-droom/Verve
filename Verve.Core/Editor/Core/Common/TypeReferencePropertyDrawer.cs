#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>类型引用属性绘制器</para>
    /// </summary>
    [CustomPropertyDrawer(typeof(TypeReference<>))]
    public class TypeReferencePropertyDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty typeNameProp = property.FindPropertyRelative("m_TypeName");
            SerializedProperty valueProp = property.FindPropertyRelative("m_Value");
            
            string key = $"{property.serializedObject.targetObject.GetInstanceID()}-{property.propertyPath}";
            if (!FoldoutStates.ContainsKey(key))
            {
                FoldoutStates[key] = false;
            }
            
            Rect foldoutRect = new Rect(
                position.x, 
                position.y, 
                position.width - 150, 
                EditorGUIUtility.singleLineHeight
            );
            
            Rect dropdownRect = new Rect(
                position.x + position.width - 150, 
                position.y, 
                150, 
                EditorGUIUtility.singleLineHeight
            );
            
            FoldoutStates[key] = EditorGUI.Foldout(foldoutRect, FoldoutStates[key], label, true);
            
            object targetObj = GetTargetObject(property);
            Type[] assignableTypes = GetAssignableTypes(targetObj);
            
            if (assignableTypes != null && assignableTypes.Length > 0)
            {
                DrawTypeDropdown(dropdownRect, typeNameProp, valueProp, assignableTypes);
            }
            
            if (FoldoutStates[key])
            {
                EditorGUI.indentLevel++;
                
                Rect valueRect = new Rect(
                    position.x, 
                    position.y + EditorGUIUtility.singleLineHeight + 2,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                
                if (valueProp != null)
                {
                    EditorGUI.PropertyField(valueRect, valueProp, new GUIContent("Value"), true);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();
        }
        
        private Type[] GetAssignableTypes(object targetObj)
        {
            if (targetObj == null) return null;
            
            MethodInfo getAssignableTypes = targetObj.GetType().GetMethod("GetAssignableTypes");
            if (getAssignableTypes == null) return null;

            return (Type[])getAssignableTypes.Invoke(targetObj, null);
        }
        
        private void DrawTypeDropdown(Rect position, SerializedProperty typeNameProp, SerializedProperty valueProp, Type[] assignableTypes)
        {
            var typeDisplayNames = new Dictionary<string, string>();
            var nameCount = new Dictionary<string, int>();
            
            foreach (var type in assignableTypes)
            {
                string simpleName = type.Name;
                if (!nameCount.ContainsKey(simpleName))
                {
                    nameCount[simpleName] = 0;
                }
                nameCount[simpleName]++;
            }
            
            foreach (var type in assignableTypes)
            {
                if (nameCount[type.Name] > 1)
                {
                    typeDisplayNames[type.AssemblyQualifiedName] = $"{type.Name} - {type.Namespace}";
                }
                else
                {
                    typeDisplayNames[type.AssemblyQualifiedName] = type.Name;
                }
            }
            
            string[] typeDisplayOptions = assignableTypes
                .Select(t => typeDisplayNames[t.AssemblyQualifiedName])
                .ToArray();
            
            string currentTypeName = typeNameProp.stringValue;
            int currentIndex = Array.FindIndex(assignableTypes, t => t.AssemblyQualifiedName == currentTypeName);
            
            if (currentIndex < 0 && assignableTypes.Length > 0)
            {
                Type baseType = assignableTypes[0].BaseType?.GenericTypeArguments[0];
                if (baseType != null)
                {
                    currentIndex = Array.FindIndex(assignableTypes, t => t == baseType);
                }
                if (currentIndex < 0) currentIndex = 0;
            }

            int newIndex = EditorGUI.Popup(position, currentIndex, typeDisplayOptions);


            if (newIndex != currentIndex && newIndex >= 0 && newIndex < assignableTypes.Length)
            {
                Type newType = assignableTypes[newIndex];

                if (valueProp != null)
                {
                    SerializedObject serializedObject = valueProp.serializedObject;
                    serializedObject.UpdateIfRequiredOrScript();
                    
#if UNITY_2019_3_OR_NEWER
                    try
                    {
                        valueProp.managedReferenceValue = null;
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        object newValue = Activator.CreateInstance(newType);
                        valueProp.managedReferenceValue = newValue;
                    }
                    catch (Exception)
                    {
                        Debug.LogError($"Type {newType.Name} must have a parameterless constructor");
                        valueProp.managedReferenceValue = null;
                    }
#endif
            
                    typeNameProp.stringValue = newType.AssemblyQualifiedName;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                }
                else
                {
                    typeNameProp.stringValue = newType.AssemblyQualifiedName;
                    typeNameProp.serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            string key = $"{property.serializedObject.targetObject.GetInstanceID()}-{property.propertyPath}";
            
            float height = EditorGUIUtility.singleLineHeight;
            
            if (FoldoutStates.TryGetValue(key, out bool isExpanded) && isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight + 2;
                
                SerializedProperty valueProp = property.FindPropertyRelative("m_Value");
                if (valueProp != null)
                {
                    height += EditorGUI.GetPropertyHeight(valueProp, true) + 2;
                }
            }
            
            return height;
        }
        
        private object GetTargetObject(SerializedProperty prop)
        {
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split('.');
            
            foreach (string element in elements)
            {
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }
            return obj;
        }
    
        private object GetValue(object source, string name, int index = -1)
        {
            if (source == null) return null;
            
            Type type = source.GetType();
            FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            
            if (field == null) return null;
            
            object value = field.GetValue(source);
            if (index >= 0 && value is Array array)
            {
                return array.GetValue(index);
            }
            return value;
        }
    }
}

#endif
