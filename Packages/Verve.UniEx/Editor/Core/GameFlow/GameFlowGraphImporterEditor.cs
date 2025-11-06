// #if UNITY_EDITOR
//
// namespace VerveEditor
// {
//     using Verve.UniEx;
//     using UnityEngine;
//     using UnityEditor;
// #if UNITY_2020_2_OR_NEWER
//     using UnityEditor.AssetImporters;
// #else
//     using UnityEditor.Experimental.AssetImporters;
// #endif
//     
//     
//     /// <summary>
//     ///   <para>流程图导入器编辑器</para>
//     /// </summary>
//     [CustomEditor(typeof(GameFlowGraphImporter))]
//     internal class GameFlowGraphImporterEditor : ScriptedImporterEditor
//     {
//         public override void OnInspectorGUI()
//         {
//             if (EditorApplication.isCompiling) return;
//             
//             GameFlowGraphAsset graphAsset = null;
//             var importer = target as GameFlowGraphImporter;
//             if (importer != null)
//             {
//                 var assetPath = AssetDatabase.GetAssetPath(importer);
//                 graphAsset = AssetDatabase.LoadAssetAtPath<GameFlowGraphAsset>(assetPath);
//             }
//         
//             // GUILayout.Space(10);
//             if (GUILayout.Button("Open Game Flow Graph", GUILayout.Height(20)))
//             {
//                 GameFlowGraphEditorWindow.CreateNewOrOpenWindow(graphAsset);
//             }
//             ApplyRevertGUI();
//         }
//     }
// }
//
// #endif