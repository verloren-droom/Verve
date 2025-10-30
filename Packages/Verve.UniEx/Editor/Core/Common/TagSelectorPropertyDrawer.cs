#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    using Verve.UniEx;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEditorInternal;
    
    
    /// <summary>
    ///   <para>标签选择器属性绘制器</para>
    /// </summary>
    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public class TagSelectorPropertyDrawer : PropertyDrawer
    {
        private static bool s_MultiSelectExpanded;
    
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool isMultiple = property.propertyType != SerializedPropertyType.String && (property.isArray && property.arrayElementType == "string");
            
            if (isMultiple)
            {
                DrawMultiSelect(position, property, label);
            }
            else
            {
                DrawSingleSelect(position, property, label);
            }
        }
    
        private void DrawSingleSelect(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var tags = InternalEditorUtility.tags;
            int selectedIndex = Array.IndexOf(tags, property.stringValue);
            if (selectedIndex < 0) selectedIndex = 0;
    
            var tagOptions = tags.Select(t => new GUIContent(t)).ToArray();
    
            selectedIndex = EditorGUI.Popup(
                position: position,
                label: label,
                selectedIndex: selectedIndex,
                displayedOptions: tagOptions
            );
            property.stringValue = tags[selectedIndex];
            
            EditorGUI.EndProperty();
        }
    
        private void DrawMultiSelect(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            s_MultiSelectExpanded = EditorGUI.Foldout(foldoutRect, s_MultiSelectExpanded, label, true);
    
            if (s_MultiSelectExpanded)
            {
                EditorGUI.indentLevel++;
                
                property.arraySize = EditorGUI.IntField(
                    new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight),
                    "Size",
                    property.arraySize);
    
                for (int i = 0; i < property.arraySize; i++)
                {
                    var element = property.GetArrayElementAtIndex(i);
                    Rect elementRect = new Rect(
                        position.x,
                        position.y + EditorGUIUtility.singleLineHeight * (i + 2),
                        position.width,
                        EditorGUIUtility.singleLineHeight);
                    
                    DrawSingleSelect(elementRect, element, new GUIContent($"Element {i}"));
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isArray && s_MultiSelectExpanded)
            {
                return EditorGUIUtility.singleLineHeight * (property.arraySize + 2);
            }
            return EditorGUIUtility.singleLineHeight;
        }
    }
}

#endif