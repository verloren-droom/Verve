#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using VerveUniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    

    internal static class ButtonEditorHelper
    {
        public static void DrawButtons(UnityEngine.Object target, MethodInfo[] methods)
        {
            foreach (var method in methods)
            {
                var buttonAttribute = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), false)[0];
                string buttonLabel = string.IsNullOrEmpty(buttonAttribute.Label) ? method.Name : buttonAttribute.Label;
    
                var parameters = method.GetParameters();
    
                if (parameters.Length > 0)
                {
                    if (buttonAttribute.Parameters == null || buttonAttribute.Parameters.Length != parameters.Length)
                    {
                        EditorGUILayout.HelpBox(
                            $"Method '{method.Name}' has parameters but ButtonAttribute does not specify matching parameters.",
                            MessageType.Warning);
                        continue;
                    }
    
                    object[] paramValues = new object[parameters.Length];
    
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        string paramName = buttonAttribute.Parameters[i];
                        var paramType = parameters[i].ParameterType;
    
                        var field = target.GetType().GetField(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field != null && field.FieldType == paramType)
                        {
                            paramValues[i] = field.GetValue(target);
                            continue;
                        }
    
                        var prop = target.GetType().GetProperty(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (prop != null && prop.PropertyType == paramType)
                        {
                            paramValues[i] = prop.GetValue(target);
                            continue;
                        }
    
                        var methodInfo = target.GetType().GetMethod(paramName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (methodInfo != null && methodInfo.ReturnType == paramType && methodInfo.GetParameters().Length == 0)
                        {
                            paramValues[i] = methodInfo.Invoke(target, null);
                            continue;
                        }
    
                        EditorGUILayout.HelpBox($"Cannot resolve parameter '{paramName}' of method '{method.Name}'.", MessageType.Error);
                        return;
                    }
    
                    if (GUILayout.Button(buttonLabel))
                    {
                        method.Invoke(target, paramValues);
                    }
                }
                else
                {
                    if (GUILayout.Button(buttonLabel))
                    {
                        method.Invoke(target, null);
                    }
                }
            }
        }
    }
}

#endif