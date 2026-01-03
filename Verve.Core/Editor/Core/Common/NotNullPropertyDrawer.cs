#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEngine;
    using UnityEditor;
    
    [CustomPropertyDrawer(typeof(NotNullAttribute))]
    public sealed class NotNullPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label);
            Rect propertyRect = new Rect(position.x, position.y, position.width, baseHeight);
            
            EditorGUI.PropertyField(propertyRect, property, label);

            bool isInvalid = property.propertyType switch
            {
                SerializedPropertyType.ObjectReference => property.objectReferenceValue == null,
                _ => false
            };
        
            if (isInvalid)
            {
                Rect errorRect = new Rect(position)
                {
                    y = position.y + baseHeight + EditorGUIUtility.standardVerticalSpacing,
                    height = EditorGUIUtility.singleLineHeight
                };
            
                EditorGUI.HelpBox(errorRect, "This property cannot be null!", MessageType.Error);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label);
        
            bool isInvalid = property.propertyType switch
            {
                SerializedPropertyType.ObjectReference => property.objectReferenceValue == null,
                _ => false
            };
        
            return isInvalid 
                ? height + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
                : height;
        }
    }
}

#endif