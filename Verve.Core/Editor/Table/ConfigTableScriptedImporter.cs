#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.AssetImporters;
    
    /// <summary>
    ///   <para>自定义配置表资源导入器。</para>
    /// </summary>
    [ScriptedImporter(1, ConfigTableModule.FileExtension)]
    internal sealed class ConfigTableScriptedImporter : ScriptedImporter
    {
        /// <inheritdoc />
        public override void OnImportAsset(AssetImportContext context)
        {
            ConfigTableDataAsset dataAsset = null;
            try
            {
                var table = ConfigTableImporter.LoadCtable(context.assetPath);
                dataAsset = ScriptableObject.CreateInstance<ConfigTableDataAsset>();
                dataAsset.name = table.tableName;
                dataAsset.SetTableData(table);
    
                var icon = EditorGUIUtility.IconContent("TextAsset Icon").image as Texture2D;
                context.AddObjectToAsset("ConfigTable", dataAsset, icon);
                context.SetMainObject(dataAsset);
            }
            catch (Exception exception)
            {
                if (dataAsset != null) DestroyImmediate(dataAsset);
                context.LogImportError("配置表导入失败。\n" + exception.Message);
            }
        }
    }
}

#endif