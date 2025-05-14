namespace VerveEditor.UniEx
{
    
#if UNITY_EDITOR
    using Verve;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;

    
    [CustomPropertyDrawer(typeof(PropertyDisableAttribute))]
    public sealed class PropertyDisableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (fieldInfo.FieldType.IsArray || (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }
        }
    }
#endif
    
}