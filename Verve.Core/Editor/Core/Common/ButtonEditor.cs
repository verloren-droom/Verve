#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;
    
    /// <summary>
    ///   <para>按钮编辑器。</para>
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
    sealed class ButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
    
            CoreEditorUtility.DrawButtons(target);
        }
    }
    
    /// <summary>
    ///   <para>脚本资源按钮编辑器。</para>
    /// </summary>
    [CustomEditor(typeof(ScriptableObject), true), CanEditMultipleObjects]
    sealed class ButtonScriptableObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
    
            CoreEditorUtility.DrawButtons(target);
        }
    }
}

#endif
