#if UNITY_EDITOR

namespace Verve.Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    

    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
    sealed class ButtonEditor : Editor
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
    sealed class ButtonScriptableObjectEditor : Editor
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