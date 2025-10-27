#if UNITY_EDITOR

namespace VerveEditor
{
    using Verve.UniEx;
    using UnityEngine;
    using UnityEditor;
    
    
    /// <summary>
    ///  <para>流程图资源编辑器</para>
    /// </summary>
    [CustomEditor(typeof(GameFlowGraphAsset))]
    internal class GameFlowGraphAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Open Game Flow Graph", GUILayout.Height(28)))
            {
                GameFlowGraphEditorWindow.OpenWindow();
            }
        }
    }
}

#endif