#if UNITY_EDITOR
    
namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public sealed class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool isCollectionType = 
                fieldInfo.FieldType.IsArray || 
                (fieldInfo.FieldType.IsGenericType && 
                 (fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>) ||
                  fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(HashSet<>) ||
                  fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))) ||
                typeof(ICollection).IsAssignableFrom(fieldInfo.FieldType);

            if (isCollectionType)
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
}
    
#endif
