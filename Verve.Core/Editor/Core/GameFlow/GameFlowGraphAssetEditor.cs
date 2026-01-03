#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    
    
    /// <summary>
    ///   <para>流程图资源编辑器</para>
    /// </summary>
    [CustomEditor(typeof(GameFlowGraphAsset))]
    internal class GameFlowGraphAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null) return;
            GameFlowGraphAsset asset = target as GameFlowGraphAsset;
    
            serializedObject.Update();
            
            using var change = new EditorGUI.ChangeCheckScope();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Root Node", asset.RootNode?.NodeName ?? "No Set");
            EditorGUILayout.LabelField("Nodes Count", (asset.nodes?.Count ?? 0).ToString());

            if (change.changed)
            {
                serializedObject.ApplyModifiedProperties();
                asset.RebuildGraphReferences();
            }
        }

        /// <summary>
        ///   <para>打开资源</para>
        /// </summary>
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int lineNumber)
        {
            string path = AssetDatabase.GetAssetPath(instanceID);
            
            if (!path.EndsWith($".asset", System.StringComparison.OrdinalIgnoreCase))
                return false;

            var asset = AssetDatabase.LoadAssetAtPath<GameFlowGraphAsset>(path);
            if (asset == null)
                return false;

            GameFlowGraphEditorWindow.CreateNewOrOpenWindow(asset);
            return true;
        }
    }
}

#endif