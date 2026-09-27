#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    ///   <para>配置表列表；提供表数据搜索和编辑入口。</para>
    /// </summary>
    internal static class ConfigTableListWindow
    {
        /// <summary>
        ///   <para>打开配置表列表。</para>
        /// </summary>
        /// <param name="activatorRect">弹窗锚定区域。</param>
        internal static void Open(Rect activatorRect)
        {
            var tables = ConfigTableImporter.GetCtableAssetPaths()
                .Select(path => (path, table: ConfigTableImporter.LoadCtable(path)))
                .OrderBy(entry => entry.table.tableName, StringComparer.OrdinalIgnoreCase).ToArray();
            var icon = EditorGUIUtility.IconContent("TextAsset Icon").image;
            var search = new EditorSearchFilter(string.Empty);
            var query = ConfigTableSearchQuery.Empty;

            CoreEditorUtility.ShowListPopup(activatorRect, tables,
                entry => CreateContent(entry.path, entry.table, icon),
                entry => ConfigTableEditorWindow.Open(entry.path, search.Text, search.RequiresExactLength, search.IsCaseSensitive),
                (entry, _) => ConfigTableSearch.MatchesTable(entry.table, entry.path, query),
                filter =>
                {
                    search = filter;
                    query = ConfigTableSearch.Parse(filter.Text, filter.RequiresExactLength, filter.IsCaseSensitive);
                });
        }

        /// <summary>
        ///   <para>创建条目内容；文件信息仅在打开列表时读取。</para>
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <param name="table">配置表数据。</param>
        /// <param name="icon">借用的图标。</param>
        private static GUIContent CreateContent(string path, ConfigTableAsset table, Texture icon)
        {
            var file = new FileInfo(ConfigTableEditorSettings.instance.GetProjectAbsolutePath(path));
            return new GUIContent(table.tableName, icon,
                $"表名：{table.tableName}\n路径：{path}\n文件信息：{Game.FileUtility.FormatFileSize(file.Length)} | {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        }
    }
}

#endif