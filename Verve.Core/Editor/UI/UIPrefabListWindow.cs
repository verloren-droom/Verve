#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    ///   <para>UI 预制体列表；展示根节点挂有 UI 组件的资源。</para>
    /// </summary>
    internal static class UIPrefabListWindow
    {
        /// <summary>
        ///   <para>打开 UI Prefab 列表弹窗。</para>
        /// </summary>
        /// <param name="activatorRect">弹窗锚定区域。</param>
        internal static void Open(Rect activatorRect)
        {
            var entries = LoadEntries().OrderBy(entry => entry.asset.name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.path, StringComparer.OrdinalIgnoreCase).ToArray();
            var icon = EditorGUIUtility.IconContent("Prefab Icon").image;
            CoreEditorUtility.ShowListPopup(activatorRect, entries,
                entry => new GUIContent($"{entry.asset.name}  [{entry.kind}]", icon, $"路径：{entry.path}"),
                entry =>
                {
                    Selection.activeObject = entry.asset;
                    EditorGUIUtility.PingObject(entry.asset);
                },
                (entry, search) => search.Matches(entry.asset.name) || search.Matches(entry.path) || search.Matches(entry.kind));
        }

        /// <summary>
        ///   <para>查询 UI 预制体；资源由 <see cref="AssetDatabase"/> 管理。</para>
        /// </summary>
        private static IEnumerable<(GameObject asset, string path, string kind)> LoadEntries()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var hasView = asset.GetComponent<UIViewComponent>() != null;
                var hasWidget = asset.GetComponent<UIWidgetComponent>() != null;
                if (hasView || hasWidget)
                    yield return (asset, path, hasView && hasWidget ? "View + Widget" : hasView ? "View" : "Widget");
            }
        }
    }
}

#endif
