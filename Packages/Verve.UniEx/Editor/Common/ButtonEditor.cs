#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using VerveUniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    
    
    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
    public class ButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
    
            var targetObject = target;
    
            var methods = targetObject.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), false).Length > 0);
    
            foreach (var method in methods)
            {
                var buttonAttribute = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), false)[0];
                string buttonLabel = string.IsNullOrEmpty(buttonAttribute.Label) ? method.Name : buttonAttribute.Label;

                if (method.GetParameters().Length > 0)
                {
                    EditorGUILayout.HelpBox($"Method '{method.Name}' has parameters. Button methods should not have parameters.", MessageType.Warning);
                    continue;
                }

                if (GUILayout.Button(buttonLabel))
                {
                    method.Invoke(targetObject, null);
                }
            }
        }
    }

}

#endif