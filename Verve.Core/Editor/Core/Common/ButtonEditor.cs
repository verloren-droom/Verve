#if UNITY_EDITOR

namespace Verve.Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    
    // [CustomPropertyDrawer(typeof(ButtonAttribute))]
    // public class ButtonAttributePropertyDrawer : PropertyDrawer
    // {
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         var targetObject = property.serializedObject.targetObject;
    //         
    //         var methods = targetObject.GetType()
    //             .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    //             .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), false).Length > 0)
    //             .ToArray();
    //
    //         float currentY = position.y;
    //         
    //         foreach (var method in methods)
    //         {
    //             var buttonAttribute = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), false)[0];
    //             string buttonLabel = string.IsNullOrEmpty(buttonAttribute.Label) ? method.Name : buttonAttribute.Label;
    //
    //             var parameters = method.GetParameters();
    //             float methodHeight = EditorGUIUtility.singleLineHeight;
    //
    //             if (parameters.Length > 0)
    //             {
    //                 if (buttonAttribute.Parameters == null || buttonAttribute.Parameters.Length != parameters.Length)
    //                 {
    //                     var warningRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight * 2);
    //                     EditorGUI.HelpBox(
    //                         warningRect,
    //                         $"Method '{method.Name}' has parameters but ButtonAttribute does not specify matching parameters.",
    //                         MessageType.Warning);
    //                     currentY += warningRect.height + EditorGUIUtility.standardVerticalSpacing;
    //                     methodHeight = warningRect.height;
    //                     continue;
    //                 }
    //
    //                 object[] paramValues = new object[parameters.Length];
    //                 bool paramsValid = true;
    //
    //                 for (int i = 0; i < parameters.Length; i++)
    //                 {
    //                     string paramName = buttonAttribute.Parameters[i];
    //                     var paramType = parameters[i].ParameterType;
    //
    //                     var field = targetObject.GetType().GetField(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //                     if (field != null && field.FieldType == paramType)
    //                     {
    //                         paramValues[i] = field.GetValue(targetObject);
    //                         continue;
    //                     }
    //
    //                     var prop = targetObject.GetType().GetProperty(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //                     if (prop != null && prop.PropertyType == paramType)
    //                     {
    //                         paramValues[i] = prop.GetValue(targetObject);
    //                         continue;
    //                     }
    //
    //                     var methodInfo = targetObject.GetType().GetMethod(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //                     if (methodInfo != null && methodInfo.ReturnType == paramType && methodInfo.GetParameters().Length == 0)
    //                     {
    //                         paramValues[i] = methodInfo.Invoke(targetObject, null);
    //                         continue;
    //                     }
    //
    //                     paramsValid = false;
    //                     break;
    //                 }
    //
    //                 if (!paramsValid)
    //                 {
    //                     var errorRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
    //                     EditorGUI.HelpBox(errorRect, $"Cannot resolve parameters for method '{method.Name}'.", MessageType.Error);
    //                     currentY += errorRect.height + EditorGUIUtility.standardVerticalSpacing;
    //                     methodHeight = errorRect.height;
    //                     continue;
    //                 }
    //
    //                 var buttonRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
    //                 if (GUI.Button(buttonRect, buttonLabel))
    //                 {
    //                     foreach (var t in property.serializedObject.targetObjects)
    //                     {
    //                         method.Invoke(t, paramValues);
    //                     }
    //                 }
    //                 currentY += buttonRect.height + EditorGUIUtility.standardVerticalSpacing;
    //             }
    //             else
    //             {
    //                 var buttonRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
    //                 if (GUI.Button(buttonRect, buttonLabel))
    //                 {
    //                     foreach (var t in property.serializedObject.targetObjects)
    //                     {
    //                         method.Invoke(t, null);
    //                     }
    //                 }
    //                 currentY += buttonRect.height + EditorGUIUtility.standardVerticalSpacing;
    //             }
    //         }
    //     }
    //
    //     public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    //     {
    //         var targetObject = property.serializedObject.targetObject;
    //         var methods = targetObject.GetType()
    //             .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    //             .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), false).Length > 0)
    //             .ToArray();
    //
    //         float totalHeight = 0;
    //
    //         foreach (var method in methods)
    //         {
    //             var buttonAttribute = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), false)[0];
    //             var parameters = method.GetParameters();
    //
    //             if (parameters.Length > 0)
    //             {
    //                 if (buttonAttribute.Parameters == null || buttonAttribute.Parameters.Length != parameters.Length)
    //                 {
    //                     totalHeight += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
    //                     continue;
    //                 }
    //
    //                 bool paramsValid = true;
    //                 for (int i = 0; i < parameters.Length; i++)
    //                 {
    //                     string paramName = buttonAttribute.Parameters[i];
    //                     var paramType = parameters[i].ParameterType;
    //
    //                     var field = targetObject.GetType().GetField(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //                     if (field != null && field.FieldType == paramType) continue;
    //
    //                     var prop = targetObject.GetType().GetProperty(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //                     if (prop != null && prop.PropertyType == paramType) continue;
    //
    //                     var methodInfo = targetObject.GetType().GetMethod(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //                     if (methodInfo != null && methodInfo.ReturnType == paramType && methodInfo.GetParameters().Length == 0) continue;
    //
    //                     paramsValid = false;
    //                     break;
    //                 }
    //
    //                 if (!paramsValid)
    //                 {
    //                     totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    //                     continue;
    //                 }
    //             }
    //
    //             totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    //         }
    //
    //         return totalHeight;
    //     }
    // }

    
    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
    public sealed class ButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
    
            var targetObject = target;
    
            var methods = targetObject.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), false).Length > 0)
                .ToArray();
    
            ButtonEditorHelper.DrawButtons(targetObject, methods);
        }
    }
    
    
    [CustomEditor(typeof(ScriptableObject), true), CanEditMultipleObjects]
    public sealed class ButtonScriptableObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
    
            var targetObject = target;
    
            var methods = targetObject.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), false).Length > 0)
                .ToArray();
    
            ButtonEditorHelper.DrawButtons(targetObject, methods);
        }
    }
}

#endif
