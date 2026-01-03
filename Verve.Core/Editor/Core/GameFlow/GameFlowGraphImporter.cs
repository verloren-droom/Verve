// #if UNITY_EDITOR
//
// namespace Verve.Editor
// {
//     using System;
//     using System.IO;
//     using Verve.UniEx;
//     using UnityEditor;
//     using UnityEngine;
//     using System.Collections.Generic;
// #if UNITY_2020_2_OR_NEWER
//     using UnityEditor.AssetImporters;
// #else
//     using UnityEditor.Experimental.AssetImporters;
// #endif
//
//
//     /// <summary>
//     ///   <para>流程图导入器</para>
//     /// </summary>
//     [ScriptedImporter(kVersion, kExtension)]
//     internal class GameFlowGraphImporter : ScriptedImporter
//     {
//         /// <summary>
//         ///   <para>流程图导入器版本</para>
//         /// </summary>
//         private const int kVersion = 1;
//         /// <summary>
//         ///   <para>流程图扩展名</para>
//         /// </summary>
//         const string kExtension = "flowgraph";
//         /// <summary>
//         ///   <para>流程图标</para>
//         /// </summary>
//         private const string kAssetIcon = "Packages/com.benfach.verve.uniex/Editor/Core/GameFlow/Icons/GameFlowGraphIcon.png";
//         /// <summary>
//         ///   <para>默认流程图布局</para>
//         /// </summary>
//         private const string kDefaultAssetLayout = @"{}";
//
//         
//         public override void OnImportAsset(AssetImportContext ctx)
//         {
//             if (ctx == null)
//                 throw new ArgumentNullException(nameof(ctx));
//
//             string text;
//             try
//             {
//                 text = File.ReadAllText(ctx.assetPath);
//             }
//             catch (Exception exception)
//             {
//                 ctx.LogImportError($"Could not read file '{ctx.assetPath}' ({exception})");
//                 return;
//             }
//             
//             var asset = ScriptableObject.CreateInstance<GameFlowGraphAsset>();
//             asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
//             
//             try
//             {
//                 if (!string.IsNullOrEmpty(text))
//                 {
//                     JsonUtility.FromJsonOverwrite(text, asset);
//                 }
//             }
//             catch (Exception exception)
//             {
//                 ctx.LogImportError($"Could not parse flow graph in JSON format from '{ctx.assetPath}' ({exception})");
//                 DestroyImmediate(asset);
//                 return;
//             }
//             
//             var icon = (Texture2D)EditorGUIUtility.Load(kAssetIcon);
//             if (icon != null)
//             {
//                 icon.hideFlags = HideFlags.HideAndDontSave;
//             }
//             
//             ctx.AddObjectToAsset("<root>", asset, icon);
//             ctx.SetMainObject(asset);
//             
//             asset.RebuildGraphReferences();
//         }
//         
//         /// <summary>
//         ///   <para>创建流程图</para>
//         /// </summary>
//         [MenuItem("Assets/Create/Verve/Game Flow Graph")]
//         public static void CreateFlowGraphAsset()
//         {
//             ProjectWindowUtil.CreateAssetWithContent(
//                 $"New Game Flow Graph.{kExtension}",
//                 kDefaultAssetLayout, 
//                 (Texture2D)EditorGUIUtility.Load(kAssetIcon));
//         }
//         
//         /// <summary>
//         ///   <para>打开流程图</para>
//         /// </summary>
//         [UnityEditor.Callbacks.OnOpenAsset]
//         public static bool OnOpenAsset(int instanceID, int lineNumber)
//         {
//             string path = AssetDatabase.GetAssetPath(instanceID);
//             if (!path.EndsWith($".{kExtension}", StringComparison.OrdinalIgnoreCase))
//                 return false;
//
//             var asset = AssetDatabase.LoadAssetAtPath<GameFlowGraphAsset>(path);
//             if (asset == null)
//                 return false;
//
//             GameFlowGraphEditorWindow.CreateNewOrOpenWindow(asset);
//             return true;
//         }
//     }
// }
//
// #endif