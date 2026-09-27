#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    ///   <para>能力表单资源列表；提供搜索、选择和快速定位入口。</para>
    /// </summary>
    internal static class CapabilitySheetListWindow
    {
        /// <summary>
        ///   <para>打开能力表单资源列表弹窗。</para>
        /// </summary>
        /// <param name="activatorRect">弹窗锚定区域。</param>
        internal static void Open(Rect activatorRect)
        {
            var entries = LoadEntries().OrderBy(entry => entry.asset.name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.path, StringComparer.OrdinalIgnoreCase).ToArray();
            var icon = EditorGUIUtility.IconContent("ScriptableObject Icon").image;
            CoreEditorUtility.ShowListPopup(
                activatorRect,
                entries,
                entry => new GUIContent(entry.asset.name, icon, $"路径：{entry.path}"),
                entry => SelectAsset(entry.asset),
                (entry, search) => search.Matches(entry.asset.name) || search.Matches(entry.path));
        }

        /// <summary>
        ///   <para>读取项目内能力表单资产；资产所有权仍由 <see cref="AssetDatabase"/> 管理。</para>
        /// </summary>
        /// <returns>能力表单资产和路径。</returns>
        private static IEnumerable<(CapabilitySheetAsset asset, string path)> LoadEntries()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CapabilitySheetAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<CapabilitySheetAsset>(path);
                if (asset != null)
                    yield return (asset, path);
            }
        }

        /// <summary>
        ///   <para>选中并定位能力表单资产。</para>
        /// </summary>
        /// <param name="asset">能力表单资产。</param>
        private static void SelectAsset(CapabilitySheetAsset asset)
        {
            if (asset == null) return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}

#endif
