#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using VerveUniEx;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
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
                .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), false).Length > 0)
                .ToArray();

            ButtonEditorHelper.DrawButtons(targetObject, methods);
        }
    }
    
    
    [CustomEditor(typeof(ScriptableObject), true), CanEditMultipleObjects]
    public class ButtonScriptableObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var targetObject = target;

            var methods = targetObject.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), false).Length > 0)
                .ToArray();

            ButtonEditorHelper.DrawButtons(targetObject, methods);
        }
    }
}

#endif
