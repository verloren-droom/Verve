#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    
    /// <summary>
    ///   <para>配置表数据资产编辑器。</para>
    /// </summary>
    [CustomEditor(typeof(ConfigTableDataAsset))]
    [CanEditMultipleObjects]
    internal sealed class ConfigTableDataAssetEditor : UnityEditor.Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    targets.Length +
                    " 个配置表资产已被选中。请选中一个配置表以查看其数据或打开配置表编辑器。",
                    MessageType.Info);
                return;
            }

            DrawDefaultInspector();
        }

        /// <summary>
        ///   <para>创建资源。</para>
        /// </summary>
        [MenuItem("Assets/Create/Verve/ConfigTableDataAsset", false, 210)]
        private static void CreateAsset()
        {
            var folder = GetSelectedFolder();
            if (!CanCreateAsset())
            {
                return;
            }

            string assetPath = EditorUtility.SaveFilePanelInProject(
                "创建配置表",
                "NewConfigTable",
                ConfigTableModule.FileExtension,
                "请输入配置表文件名。",
                folder);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            assetPath = Game.PathUtility.Normalize(assetPath);
            if (!string.Equals(Path.GetDirectoryName(assetPath), ConfigTableEditorSettings.ClientTableFolder,
                    StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("创建配置表失败",
                    "配置表只能创建在：" +
                    ConfigTableEditorSettings.ClientTableFolder,
                    "确定");
                return;
            }

            if (File.Exists(ConfigTableEditorSettings.instance.GetProjectAbsolutePath(assetPath)))
            {
                EditorUtility.DisplayDialog("创建配置表失败",
                    "配置表已存在：" + assetPath,
                    "确定");
                return;
            }

            var tableName = Path.GetFileNameWithoutExtension(assetPath);
            var table = new ConfigTableAsset
            {
                tableName = tableName,
                fields = new[] { "id", "value" },
                types = new[] { "int", "string" },
                comments = new[] { string.Empty, string.Empty },
                references = new[] { string.Empty, string.Empty },
                rows = new[]
                {
                    new ConfigTableRecord
                    {
                        id = "0",
                        values = new[] { "0", string.Empty }
                    }
                }
            };

            try
            {
                ConfigTableImporter.SaveCtable(assetPath, table);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            catch (Exception exception)
            {
                Game.LogError("创建 ." + ConfigTableModule.FileExtension +
                               " 配置表失败。\n" + exception.Message);
            }
        }

        /// <summary>
        ///   <para>判断是否允许创建资源。</para>
        /// </summary>
        [MenuItem("Assets/Create/Verve/ConfigTableDataAsset", true)]
        private static bool CanCreateAsset()
        {
            return string.Equals(GetSelectedFolder(), ConfigTableEditorSettings.ClientTableFolder,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///   <para>获取选中目录。</para>
        /// </summary>
        private static string GetSelectedFolder()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(selectedPath))
            {
                return selectedPath;
            }

            string selectedFolder =
                string.IsNullOrEmpty(selectedPath) ? string.Empty : Path.GetDirectoryName(selectedPath);
            return AssetDatabase.IsValidFolder(selectedFolder) ? selectedFolder : "Assets";
        }
    }
}

#endif