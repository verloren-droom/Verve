#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using VerveUniEx;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    

    [CustomPropertyDrawer(typeof(RequireComponentOnGameObjectAttribute))]
    public class RequireComponentOnGameObjectPropertyDrawer : PropertyDrawer
    {
        private const float WarningPadding = 2f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float propertyHeight = EditorGUI.GetPropertyHeight(property, label);
            Rect propertyRect = new Rect(position.x, position.y, position.width, propertyHeight);
            
            EditorGUI.PropertyField(propertyRect, property, label);
            
            var requireAttr = attribute as RequireComponentOnGameObjectAttribute;
            if (requireAttr == null || requireAttr.RequiredType == null) return;
            
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogWarning($"[RequireComponentOnGameObject] 特性只能用于 GameObject 或 Component 字段");
                return;
            }
            
            GameObject targetGameObject = null;
            if (property.objectReferenceValue is GameObject go)
            {
                targetGameObject = go;
            }
            else if (property.objectReferenceValue is Component component)
            {
                targetGameObject = component.gameObject;
            }
            
            bool componentMissing = true;
            if (targetGameObject != null)
            {
                if (targetGameObject.GetComponent(requireAttr.RequiredType) != null)
                {
                    componentMissing = false;
                }
                else if (requireAttr.RequiredType.IsClass)
                {
                    var components = targetGameObject.GetComponents<Component>();
                    if (components.Any(c => c.GetType().IsSubclassOf(requireAttr.RequiredType)))
                    {
                        componentMissing = false;
                    }
                }
                else if (requireAttr.RequiredType.IsInterface)
                {
                    var components = targetGameObject.GetComponents<Component>();
                    if (components.Any(c => requireAttr.RequiredType.IsInstanceOfType(c)))
                    {
                        componentMissing = false;
                    }
                }
            }
            
            if (targetGameObject != null && componentMissing)
            {
                string warningText = $"GameObject '{targetGameObject.name}' 缺少必要组件: {requireAttr.RequiredType.Name}";
                GUIContent warningContent = new GUIContent(warningText);
                
                float warningHeight = EditorStyles.helpBox.CalcHeight(
                    warningContent, 
                    position.width - EditorGUI.indentLevel * 15f
                );
                
                Rect warningRect = new Rect(
                    position.x,
                    position.y + propertyHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    warningHeight
                );
                
                EditorGUI.HelpBox(
                    warningRect, 
                    warningText, 
                    MessageType.Warning
                );
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label);

            if (attribute is not RequireComponentOnGameObjectAttribute requireAttr || requireAttr.RequiredType == null) return height;
            
            GameObject targetGameObject = null;
            if (property.objectReferenceValue is GameObject go)
            {
                targetGameObject = go;
            }
            else if (property.objectReferenceValue is Component component)
            {
                targetGameObject = component.gameObject;
            }
            
            if (targetGameObject != null)
            {
                bool componentMissing = true;
                
                if (targetGameObject.GetComponent(requireAttr.RequiredType) != null)
                {
                    componentMissing = false;
                }
                else if (requireAttr.RequiredType.IsClass)
                {
                    var components = targetGameObject.GetComponents<Component>();
                    if (components.Any(c => c.GetType().IsSubclassOf(requireAttr.RequiredType)))
                    {
                        componentMissing = false;
                    }
                }
                {
                    var components = targetGameObject.GetComponents<Component>();
                    if (components.Any(c => requireAttr.RequiredType.IsInstanceOfType(c)))
                    {
                        componentMissing = false;
                    }
                }
                
                if (componentMissing)
                {
                    string warningText = $"GameObject '{targetGameObject.name}' 缺少必要组件: {requireAttr.RequiredType.Name}";
                    GUIContent warningContent = new GUIContent(warningText);
                    
                    float estimatedWidth = EditorGUIUtility.currentViewWidth > 0 ? 
                        EditorGUIUtility.currentViewWidth - 30f : 300f;
                    
                    float warningHeight = EditorStyles.helpBox.CalcHeight(
                        warningContent, 
                        estimatedWidth - EditorGUI.indentLevel * 15f
                    );
                    
                    height += warningHeight + EditorGUIUtility.standardVerticalSpacing + WarningPadding;
                }
            }
            
            return height;
        }
    }
}

#endif